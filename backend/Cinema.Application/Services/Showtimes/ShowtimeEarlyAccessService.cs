using Cinema.Application.DTOs.Showtimes;
using Cinema.Application.Interfaces.Audit;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Showtimes;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Showtimes;

public class ShowtimeEarlyAccessService(IAppDbContext db, IAuditLogger auditLogger)
    : IShowtimeEarlyAccessService
{
    public async Task<ShowtimeEarlyAccessSettingDto> GetAsync(
        int showtimeId,
        CancellationToken cancellationToken
    )
    {
        var setting =
            await db
                .ShowtimeEventSettings.AsNoTracking()
                .FirstOrDefaultAsync(s => s.ShowtimeId == showtimeId, cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_ShowtimeEvent);
        return ToDto(setting);
    }

    public async Task<ShowtimeEarlyAccessSettingDto> UpsertAsync(
        int showtimeId,
        UpsertShowtimeEarlyAccessRequest request,
        CancellationToken cancellationToken
    )
    {
        var tierIds = (request.MembershipTierIds ?? []).Distinct().Order().ToList();
        if (tierIds.Count > 0)
        {
            var found = await db.MembershipTiers.CountAsync(
                t => tierIds.Contains(t.TierId),
                cancellationToken
            );
            if (found != tierIds.Count)
                throw new NotFoundException(Messages.NotFound_MembershipTier);
        }

        var (showtime, setting) = await LoadEditableAsync(showtimeId, cancellationToken);
        var generalSaleStart = request.GeneralSaleStartTime;
        if (generalSaleStart <= DateTime.UtcNow || generalSaleStart >= showtime.StartTime)
            throw new BusinessRuleException(Messages.Rule_EventGeneralSaleTimeInvalid);

        var before = ToDto(setting);
        setting.GeneralSaleStartTime = generalSaleStart;
        setting.EarlyAccessTierIds = tierIds;
        setting.EarlyAccessPaidMembers = request.PaidMembers;
        setting.EarlyAccessMovieInterest = request.MovieInterest;
        setting.UpdatedAt = DateTime.UtcNow;

        var after = ToDto(setting);
        auditLogger.Record(
            AuditActions.Update,
            AuditEntityTypes.ShowtimeEvent,
            showtimeId.ToString(),
            before,
            after
        );
        await db.SaveChangesAsync(cancellationToken);
        return after;
    }

    public async Task<int> RemoveAsync(int showtimeId, CancellationToken cancellationToken)
    {
        var (_, setting) = await LoadEditableAsync(showtimeId, cancellationToken);
        if (setting.GeneralSaleStartTime is null)
            throw new NotFoundException(Messages.NotFound_ShowtimeEventEarlyAccess);

        var before = ToDto(setting);
        setting.GeneralSaleStartTime = null;
        setting.EarlyAccessTierIds = [];
        setting.EarlyAccessPaidMembers = false;
        setting.EarlyAccessMovieInterest = false;
        setting.UpdatedAt = DateTime.UtcNow;

        auditLogger.Record(
            AuditActions.Update,
            AuditEntityTypes.ShowtimeEvent,
            showtimeId.ToString(),
            before,
            ToDto(setting)
        );
        await db.SaveChangesAsync(cancellationToken);
        return showtimeId;
    }

    public async Task EnsureCanHoldAsync(
        int showtimeId,
        int? userId,
        CancellationToken cancellationToken
    )
    {
        var evaluations = await EvaluateAsync([showtimeId], userId, cancellationToken);
        if (evaluations.TryGetValue(showtimeId, out var evaluation) && !evaluation.CanBook)
            throw new BusinessRuleException(
                IneligibleMessage(evaluation.Rule.GeneralSaleStartTime),
                ApiErrorCodes.EventEarlyAccessOnly
            );
    }

    public async Task<IReadOnlyDictionary<int, ShowtimeEarlyAccessDto>> GetForViewerAsync(
        IReadOnlyCollection<int> showtimeIds,
        int? viewerUserId,
        CancellationToken cancellationToken
    )
    {
        var evaluations = await EvaluateAsync(showtimeIds, viewerUserId, cancellationToken);
        if (evaluations.Count == 0)
            return new Dictionary<int, ShowtimeEarlyAccessDto>();

        var tierIds = evaluations.Values.SelectMany(e => e.Rule.TierIds).Distinct().ToList();
        var tierNames =
            tierIds.Count == 0
                ? new Dictionary<int, string>()
                : await db
                    .MembershipTiers.AsNoTracking()
                    .Where(t => tierIds.Contains(t.TierId))
                    .ToDictionaryAsync(t => t.TierId, t => t.TierName, cancellationToken);

        return evaluations.ToDictionary(
            e => e.Key,
            e => new ShowtimeEarlyAccessDto(
                e.Value.Rule.GeneralSaleStartTime,
                e.Value.CanBook,
                e.Value.CanBook ? null : IneligibleMessage(e.Value.Rule.GeneralSaleStartTime),
                [
                    .. e.Value.Rule.TierIds.Where(tierNames.ContainsKey).Select(id => tierNames[id]),
                ],
                e.Value.Rule.PaidMembers,
                e.Value.Rule.MovieInterest
            )
        );
    }

    private async Task<Dictionary<int, Evaluation>> EvaluateAsync(
        IReadOnlyCollection<int> showtimeIds,
        int? userId,
        CancellationToken cancellationToken
    )
    {
        var result = new Dictionary<int, Evaluation>();
        if (showtimeIds.Count == 0)
            return result;

        var now = DateTime.UtcNow;
        var rules = (
            await db
                .ShowtimeEventSettings.AsNoTracking()
                .Where(s =>
                    showtimeIds.Contains(s.ShowtimeId)
                    && s.GeneralSaleStartTime != null
                    && s.GeneralSaleStartTime > now
                )
                .Select(s => new
                {
                    s.ShowtimeId,
                    s.Showtime.MovieId,
                    GeneralSaleStartTime = s.GeneralSaleStartTime!.Value,
                    s.EarlyAccessTierIds,
                    s.EarlyAccessPaidMembers,
                    s.EarlyAccessMovieInterest,
                })
                .ToListAsync(cancellationToken)
        )
            .Select(s => new Rule(
                s.ShowtimeId,
                s.MovieId,
                s.GeneralSaleStartTime,
                s.EarlyAccessTierIds,
                s.EarlyAccessPaidMembers,
                s.EarlyAccessMovieInterest
            ))
            .ToList();
        if (rules.Count == 0)
            return result;

        int? viewerTierId = null;
        var hasPaidMembership = false;
        var interestedMovieIds = new HashSet<int>();
        if (userId is { } id)
        {
            if (rules.Any(r => r.TierIds.Count > 0))
                viewerTierId = (
                    await LoyaltyWindow.GetStandingAsync(db, id, now, cancellationToken)
                ).Tier?.TierId;

            if (rules.Any(r => r.PaidMembers))
                hasPaidMembership = await db
                    .MembershipSubscriptions.AsNoTracking()
                    .IgnoreQueryFilters()
                    .AnyAsync(
                        s =>
                            s.UserId == id
                            && s.Status == MembershipSubscriptionStatus.Active
                            && s.ValidFrom <= now
                            && s.ValidTo > now,
                        cancellationToken
                    );

            var interestMovieIds = rules
                .Where(r => r.MovieInterest)
                .Select(r => r.MovieId)
                .Distinct()
                .ToList();
            if (interestMovieIds.Count > 0)
                interestedMovieIds = (
                    await db
                        .MovieInterests.AsNoTracking()
                        .Where(i => i.UserId == id && interestMovieIds.Contains(i.MovieId))
                        .Select(i => i.MovieId)
                        .ToListAsync(cancellationToken)
                ).ToHashSet();
        }

        foreach (var rule in rules)
        {
            var canBook =
                userId is not null
                && (
                    (viewerTierId is { } tierId && rule.TierIds.Contains(tierId))
                    || (rule.PaidMembers && hasPaidMembership)
                    || (rule.MovieInterest && interestedMovieIds.Contains(rule.MovieId))
                );
            result[rule.ShowtimeId] = new Evaluation(rule, canBook);
        }
        return result;
    }

    private async Task<(Showtime Showtime, ShowtimeEventSetting Setting)> LoadEditableAsync(
        int showtimeId,
        CancellationToken cancellationToken
    )
    {
        var showtime =
            await db
                .Showtimes.Include(s => s.EventSetting)
                .FirstOrDefaultAsync(s => s.ShowtimeId == showtimeId, cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_Showtime);
        var setting =
            showtime.EventSetting
            ?? throw new NotFoundException(Messages.NotFound_ShowtimeEvent);

        if (showtime.Status != ShowtimeStatus.Scheduled || showtime.StartTime <= DateTime.UtcNow)
            throw new BusinessRuleException(Messages.Rule_ShowtimeEventEarlyAccessStatusInvalid);

        return (showtime, setting);
    }

    private static string IneligibleMessage(DateTime generalSaleStartTimeUtc) =>
        string.Format(
            Messages.Rule_EventEarlyAccessOnly,
            BusinessTime.ToLocal(generalSaleStartTimeUtc).ToString(DateTimePatterns.DateHourMinute)
        );

    private static ShowtimeEarlyAccessSettingDto ToDto(ShowtimeEventSetting s) =>
        ToDto(
            s.ShowtimeId,
            s.GeneralSaleStartTime,
            s.EarlyAccessTierIds,
            s.EarlyAccessPaidMembers,
            s.EarlyAccessMovieInterest
        );

    private static ShowtimeEarlyAccessSettingDto ToDto(
        int showtimeId,
        DateTime? generalSaleStartTime,
        IEnumerable<int> tierIds,
        bool paidMembers,
        bool movieInterest
    ) => new(showtimeId, generalSaleStartTime, [.. tierIds.Order()], paidMembers, movieInterest);

    private sealed record Rule(
        int ShowtimeId,
        int MovieId,
        DateTime GeneralSaleStartTime,
        List<int> TierIds,
        bool PaidMembers,
        bool MovieInterest
    );

    private sealed record Evaluation(Rule Rule, bool CanBook);
}
