using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Segments;
using Cinema.Application.Services.Recommendations;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Segments;

public class CustomerSegmentService(IAppDbContext db) : ICustomerSegmentService
{
    private const long RefreshLockKey = 1_522_001;

    private static readonly OrderType[] SpendOrderTypes =
    [
        OrderType.Ticket,
        OrderType.Combo,
        OrderType.Supplement,
        OrderType.Exchange,
    ];

    private sealed record OrderStats(DateTime First, DateTime Last, int LookbackCount);

    private sealed record SpendStats(decimal Total, decimal Lookback);

    private sealed record PointStats(int Expiring, DateTime? ExpireAt);

    private sealed record CustomerRow(int UserId, DateTime CreatedAt, bool Personalized);

    public Task RefreshAsync(CancellationToken cancellationToken) =>
        db.InTransactionAsync(
            async () =>
            {
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT pg_advisory_xact_lock({RefreshLockKey})",
                    cancellationToken
                );

                var now = DateTime.UtcNow;
                var settings = await LoadSettingsAsync(cancellationToken);
                var rules = new SegmentRules(
                    settings.SegmentNewCustomerDays,
                    settings.SegmentLapsedDays,
                    settings.SegmentRegularMinOrders,
                    settings.SegmentLookbackDays,
                    settings.SegmentHighValueMinScore,
                    settings.SegmentLtvProjectionMonths,
                    settings.SegmentTierUpgradePercent,
                    settings.SegmentPreferenceMinViewings,
                    settings.SegmentPreferenceMinSharePercent
                );

                var inputs = await LoadInputsAsync(settings, rules, now, cancellationToken);
                var tiers = await db
                    .MembershipTiers.AsNoTracking()
                    .Select(t => new SegmentTierInput(t.TierId, t.MinPoints))
                    .ToListAsync(cancellationToken);

                var results = CustomerSegmentCalculator.Calculate(inputs, tiers, rules, now);
                await ReplaceAsync(results, now, cancellationToken);
            },
            cancellationToken
        );

    private async Task<CinemaSettings> LoadSettingsAsync(CancellationToken cancellationToken) =>
        await db
            .CinemaSettings.AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.Id == AppDbContextExtensions.CinemaSettingsId,
                cancellationToken
            ) ?? new CinemaSettings();

    private async Task<List<SegmentCustomerInput>> LoadInputsAsync(
        CinemaSettings settings,
        SegmentRules rules,
        DateTime now,
        CancellationToken cancellationToken
    )
    {
        var since = now.AddDays(-rules.LookbackDays);

        var customers = await db
            .Users.AsNoTracking()
            .Where(u => u.IsActive && u.Role.RoleName == Roles.Customer)
            .Select(u => new CustomerRow(
                u.UserId,
                u.CreatedAt,
                u.CustomerProfile != null && u.CustomerProfile.PersonalizedRecommendations
            ))
            .ToListAsync(cancellationToken);

        var orders = await LoadOrderStatsAsync(since, cancellationToken);
        var spends = await LoadSpendStatsAsync(since, cancellationToken);
        var activePoints = await LoadActivePointsAsync(now, cancellationToken);
        var expiring = await LoadExpiringPointsAsync(
            now,
            settings.PointsExpiryReminderDays,
            cancellationToken
        );
        var viewings = await LoadViewingsAsync(now, cancellationToken);

        return customers
            .Select(c =>
            {
                var order = orders.GetValueOrDefault(c.UserId);
                var spend = spends.GetValueOrDefault(c.UserId);
                var points = expiring.GetValueOrDefault(c.UserId);
                var views = c.Personalized ? viewings.GetValueOrDefault(c.UserId) : null;
                return new SegmentCustomerInput(
                    c.UserId,
                    c.CreatedAt,
                    order?.First,
                    order?.Last,
                    order?.LookbackCount ?? 0,
                    Math.Max(0m, spend?.Lookback ?? 0m),
                    Math.Max(0m, spend?.Total ?? 0m),
                    activePoints.GetValueOrDefault(c.UserId),
                    points?.Expiring ?? 0,
                    points?.ExpireAt,
                    views?.Genres ?? [],
                    views?.ScreenTypes ?? []
                );
            })
            .ToList();
    }

    private async Task<Dictionary<int, OrderStats>> LoadOrderStatsAsync(
        DateTime since,
        CancellationToken cancellationToken
    ) =>
        await db
            .Orders.AsNoTracking()
            .Where(o =>
                o.UserId != null
                && o.Status == OrderStatus.Confirmed
                && (o.Type == OrderType.Ticket || o.Type == OrderType.Combo)
            )
            .GroupBy(o => o.UserId!.Value)
            .Select(g => new
            {
                UserId = g.Key,
                First = g.Min(o => o.CreatedAt),
                Last = g.Max(o => o.CreatedAt),
                LookbackCount = g.Count(o => o.CreatedAt >= since),
            })
            .ToDictionaryAsync(
                x => x.UserId,
                x => new OrderStats(x.First, x.Last, x.LookbackCount),
                cancellationToken
            );

    private async Task<Dictionary<int, SpendStats>> LoadSpendStatsAsync(
        DateTime since,
        CancellationToken cancellationToken
    ) =>
        await db
            .OrderLines.AsNoTracking()
            .ExcludingExchangedOut(db)
            .Where(l =>
                (l.LineType == OrderLineType.Ticket || l.LineType == OrderLineType.Combo)
                && l.Order.UserId != null
                && l.Order.Status == OrderStatus.Confirmed
                && SpendOrderTypes.Contains(l.Order.Type)
            )
            .GroupBy(l => l.Order.UserId!.Value)
            .Select(g => new
            {
                UserId = g.Key,
                Total = g.Sum(l => l.PaidAmount - l.RefundedAmount),
                Lookback = g.Sum(l =>
                    l.Order.CreatedAt >= since ? l.PaidAmount - l.RefundedAmount : 0m
                ),
            })
            .ToDictionaryAsync(
                x => x.UserId,
                x => new SpendStats(x.Total, x.Lookback),
                cancellationToken
            );

    private async Task<Dictionary<int, int>> LoadActivePointsAsync(
        DateTime now,
        CancellationToken cancellationToken
    )
    {
        var months = await LoyaltyWindow.LoadMonthsAsync(db, cancellationToken);
        var cutoff = LoyaltyWindow.Cutoff(now, months);
        return await db
            .LoyaltyPointEntries.AsNoTracking()
            .Where(e => e.Type == LoyaltyLotType.Earn && e.EarnedAt > cutoff)
            .GroupBy(e => e.UserId)
            .Select(g => new { UserId = g.Key, Points = g.Sum(e => e.Points - e.RevokedPoints) })
            .ToDictionaryAsync(x => x.UserId, x => x.Points, cancellationToken);
    }

    private async Task<Dictionary<int, PointStats>> LoadExpiringPointsAsync(
        DateTime now,
        int windowDays,
        CancellationToken cancellationToken
    )
    {
        var windowEnd = now.AddDays(windowDays);
        return await db
            .LoyaltyPointEntries.AsNoTracking()
            .Where(e => e.RemainingPoints > 0 && e.ExpiresAt > now && e.ExpiresAt <= windowEnd)
            .GroupBy(e => e.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                Expiring = g.Sum(e => e.RemainingPoints),
                ExpireAt = g.Min(e => e.ExpiresAt),
            })
            .ToDictionaryAsync(
                x => x.UserId,
                x => new PointStats(x.Expiring, x.ExpireAt),
                cancellationToken
            );
    }

    private sealed record UserViewings(
        List<IReadOnlyList<int>> Genres,
        List<int> ScreenTypes
    );

    private async Task<Dictionary<int, UserViewings>> LoadViewingsAsync(
        DateTime now,
        CancellationToken cancellationToken
    )
    {
        var rows = await db
            .OrderLines.AsNoTracking()
            .WatchedViewings(db, now)
            .Where(l => l.Order.UserId != null)
            .Select(l => new
            {
                UserId = l.Order.UserId!.Value,
                ShowtimeId = l.ShowtimeId!.Value,
                l.Showtime!.MovieId,
                l.Showtime.Room.ScreenTypeId,
            })
            .Distinct()
            .ToListAsync(cancellationToken);
        if (rows.Count == 0)
            return [];

        var genresByMovie = (
            await db
                .MovieGenres.AsNoTracking()
                .Select(g => new { g.MovieId, g.GenreId })
                .ToListAsync(cancellationToken)
        )
            .GroupBy(g => g.MovieId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<int>)[.. g.Select(x => x.GenreId)]);

        var result = new Dictionary<int, UserViewings>();
        foreach (var row in rows)
        {
            if (!result.TryGetValue(row.UserId, out var views))
            {
                views = new UserViewings([], []);
                result[row.UserId] = views;
            }

            views.Genres.Add(genresByMovie.GetValueOrDefault(row.MovieId) ?? []);
            if (row.ScreenTypeId is { } screenTypeId)
                views.ScreenTypes.Add(screenTypeId);
        }
        return result;
    }

    private async Task ReplaceAsync(
        List<SegmentResult> results,
        DateTime now,
        CancellationToken cancellationToken
    )
    {
        await db
            .CustomerSegmentProfiles.Where(p =>
                !db.Users.Any(u =>
                    u.UserId == p.UserId && u.IsActive && u.Role.RoleName == Roles.Customer
                )
            )
            .ExecuteDeleteAsync(cancellationToken);

        var existing = await db.CustomerSegmentProfiles.ToDictionaryAsync(
            p => p.UserId,
            cancellationToken
        );

        foreach (var result in results)
        {
            if (!existing.TryGetValue(result.UserId, out var profile))
            {
                profile = new CustomerSegmentProfile { UserId = result.UserId };
                Apply(profile, result, now);
                db.CustomerSegmentProfiles.Add(profile);
                continue;
            }

            if (ToResult(profile) != result)
                Apply(profile, result, now);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static void Apply(CustomerSegmentProfile profile, SegmentResult r, DateTime now)
    {
        profile.ActivityStage = r.ActivityStage;
        profile.FirstOrderAt = r.FirstOrderAt;
        profile.LastOrderAt = r.LastOrderAt;
        profile.DaysSinceLastOrder = r.DaysSinceLastOrder;
        profile.LookbackOrderCount = r.LookbackOrderCount;
        profile.LookbackSpent = r.LookbackSpent;
        profile.TotalSpent = r.TotalSpent;
        profile.RecencyScore = r.RecencyScore;
        profile.FrequencyScore = r.FrequencyScore;
        profile.MonetaryScore = r.MonetaryScore;
        profile.IsHighSpender = r.IsHighSpender;
        profile.IsHighFrequency = r.IsHighFrequency;
        profile.EstimatedLifetimeValue = r.EstimatedLifetimeValue;
        profile.PreferredGenreId = r.PreferredGenreId;
        profile.PreferredScreenTypeId = r.PreferredScreenTypeId;
        profile.ExpiringPoints = r.ExpiringPoints;
        profile.PointsExpireAt = r.PointsExpireAt;
        profile.NextTierId = r.NextTierId;
        profile.PointsToNextTier = r.PointsToNextTier;
        profile.IsNearTierUpgrade = r.IsNearTierUpgrade;
        profile.UpdatedAt = now;
    }

    private static SegmentResult ToResult(CustomerSegmentProfile p) =>
        new(
            p.UserId,
            p.ActivityStage,
            p.FirstOrderAt,
            p.LastOrderAt,
            p.DaysSinceLastOrder,
            p.LookbackOrderCount,
            p.LookbackSpent,
            p.TotalSpent,
            p.RecencyScore,
            p.FrequencyScore,
            p.MonetaryScore,
            p.IsHighSpender,
            p.IsHighFrequency,
            p.EstimatedLifetimeValue,
            p.PreferredGenreId,
            p.PreferredScreenTypeId,
            p.ExpiringPoints,
            p.PointsExpireAt,
            p.NextTierId,
            p.PointsToNextTier,
            p.IsNearTierUpgrade
        );
}
