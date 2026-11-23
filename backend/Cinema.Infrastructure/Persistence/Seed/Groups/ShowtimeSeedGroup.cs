using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Infrastructure.Persistence.Seed.Groups;

internal sealed class ShowtimeSeedGroup : ISeedGroup
{
    private sealed record SpecialSpec(
        string ScreenType,
        int RoomOrdinal,
        DateTime Start,
        ShowtimeStatus Status
    );

    public string Name => "showtimes";

    public async Task SeedAsync(SeedContext context, CancellationToken cancellationToken)
    {
        var db = context.Db;
        var now = DateTime.UtcNow;

        var firstSeed =
            context.SeedDemoScenarios && !await db.Showtimes.AnyAsync(cancellationToken);
        var plan = await ShowtimeScheduler.LoadAsync(db, now, cancellationToken);
        if (!plan.CanSchedule)
        {
            context.Logger.LogWarning(
                "Skip seeding showtimes: no NowShowing movies or no active room with seats."
            );
            return;
        }

        var created = 0;
        if (firstSeed)
            created += await SeedSpecialsAsync(db, plan, now, cancellationToken);

        created += await SeedFutureAsync(db, plan, now, cancellationToken);

        if (context.SeedDemoScenarios)
            created += await SeedPastAsync(db, plan, now, cancellationToken);

        if (created > 0)
            context.Logger.LogInformation("Seeded {Count} showtimes.", created);
    }

    private static async Task<int> SeedSpecialsAsync(
        AppDbContext db,
        ShowtimePlan plan,
        DateTime now,
        CancellationToken cancellationToken
    )
    {
        var nowMinute = new DateTime(now.Ticks - now.Ticks % TimeSpan.TicksPerMinute, now.Kind);
        var today = ShowtimeScheduler.LocalDay(now);

        SpecialSpec[] specs =
        [
            new(
                RoomCatalogSeedGroup.ScreenType2D,
                0,
                nowMinute.AddMinutes(-20),
                ShowtimeStatus.Scheduled
            ),
            new(
                RoomCatalogSeedGroup.ScreenType2D,
                1,
                nowMinute.AddMinutes(20),
                ShowtimeStatus.Scheduled
            ),
            new(
                RoomCatalogSeedGroup.ScreenTypeImax,
                0,
                nowMinute.AddMinutes(90),
                ShowtimeStatus.Scheduled
            ),
            new(
                RoomCatalogSeedGroup.ScreenType3D,
                0,
                BusinessTime.ToUtc(today.AddDays(1), new TimeOnly(19, 0)),
                ShowtimeStatus.Scheduled
            ),
            new(
                RoomCatalogSeedGroup.ScreenTypeImax,
                0,
                BusinessTime.ToUtc(today.AddDays(2), new TimeOnly(15, 0)),
                ShowtimeStatus.Cancelled
            ),
        ];

        var created = new List<Showtime>();
        for (var i = 0; i < specs.Length; i++)
        {
            var spec = specs[i];
            var preferred = plan
                .Rooms.Where(r => r.ScreenType?.Name == spec.ScreenType)
                .Skip(spec.RoomOrdinal)
                .FirstOrDefault();
            var movie = plan.Movies[i % plan.Movies.Count];

            var showtime = ShowtimeScheduler.PlanSpecial(
                plan,
                movie,
                preferred,
                spec.Start,
                spec.Status,
                now
            );
            if (showtime is not null)
                created.Add(showtime);
        }

        return await SaveAsync(db, created, cancellationToken);
    }

    private static async Task<int> SeedFutureAsync(
        AppDbContext db,
        ShowtimePlan plan,
        DateTime now,
        CancellationToken cancellationToken
    )
    {
        var cutoff = ShowtimeScheduler.LeadCutoff(plan, now);
        var total = 0;

        for (var offset = 0; offset <= plan.LookaheadDays; offset++)
        {
            var rotation = offset * 3;
            var window = new PlanWindow(NotBefore: offset == 0 ? cutoff : null);
            var day = ShowtimeScheduler.PlanDay(
                plan,
                ShowtimeScheduler.LocalDay(now, offset),
                plan.Movies,
                window,
                ShowtimeScheduler.MinShowsPerDay,
                ref rotation,
                now
            );
            total += await SaveAsync(db, day, cancellationToken);
        }

        return total;
    }

    private static async Task<int> SeedPastAsync(
        AppDbContext db,
        ShowtimePlan plan,
        DateTime now,
        CancellationToken cancellationToken
    )
    {
        var total = 0;

        for (var offset = -ShowtimeScheduler.PastDays; offset <= 0; offset++)
        {
            var rotation = (offset + ShowtimeScheduler.PastDays) * 3;
            var isToday = offset == 0;
            var day = ShowtimeScheduler.PlanDay(
                plan,
                ShowtimeScheduler.LocalDay(now, offset),
                plan.Movies,
                isToday ? new PlanWindow(NotAfter: now) : new PlanWindow(),
                isToday ? ShowtimeScheduler.MaxShowsPerDay : ShowtimeScheduler.MinShowsPerDay,
                ref rotation,
                now
            );
            total += await SaveAsync(db, day, cancellationToken);
        }

        return total;
    }

    private static async Task<int> SaveAsync(
        AppDbContext db,
        List<Showtime> showtimes,
        CancellationToken cancellationToken
    )
    {
        if (showtimes.Count == 0)
            return 0;

        db.Showtimes.AddRange(showtimes);
        await db.SaveChangesAsync(cancellationToken);
        return showtimes.Count;
    }
}
