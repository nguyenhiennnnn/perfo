using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Reports;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Retention;

public class RetentionStatsService(IAppDbContext db) : IRetentionStatsService
{
    private const long RefreshLockKey = 1_522_002;

    public Task RefreshAsync(CancellationToken cancellationToken) =>
        db.InTransactionAsync(
            async () =>
            {
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT pg_advisory_xact_lock({RefreshLockKey})",
                    cancellationToken
                );

                var now = DateTime.UtcNow;
                var currentMonth = RetentionRules.MonthOf(now);
                var from = await ResolveFromMonthAsync(currentMonth, cancellationToken);
                if (from is null)
                    return;

                var existing = await db
                    .CustomerRetentionStats.Where(s => s.CohortMonth >= from.Value)
                    .ToDictionaryAsync(s => (s.CohortMonth, s.MonthOffset), cancellationToken);

                for (var cohort = from.Value; cohort <= currentMonth; cohort = cohort.AddMonths(1))
                {
                    var cohortStart = RetentionRules.StartUtc(cohort);
                    var cohortEnd = RetentionRules.StartUtc(cohort.AddMonths(1));
                    var customers = db
                        .Users.AsNoTracking()
                        .Where(u =>
                            u.Role.RoleName == Roles.Customer
                            && u.CreatedAt >= cohortStart
                            && u.CreatedAt < cohortEnd
                        );
                    var size = await customers.CountAsync(cancellationToken);

                    for (var offset = 1; offset <= RetentionRules.MaxMonthOffset; offset++)
                    {
                        var month = cohort.AddMonths(offset);
                        if (!RetentionRules.HasStarted(month, now))
                            break;

                        var start = RetentionRules.StartUtc(month);
                        var end = RetentionRules.StartUtc(month.AddMonths(1));
                        var returned =
                            size == 0
                                ? 0
                                : await customers.CountAsync(
                                    u =>
                                        db.Orders.Any(o =>
                                            o.UserId == u.UserId
                                            && o.Status == OrderStatus.Confirmed
                                            && o.Type == OrderType.Ticket
                                            && o.CreatedAt >= start
                                            && o.CreatedAt < end
                                        ),
                                    cancellationToken
                                );

                        if (existing.TryGetValue((cohort, offset), out var stat))
                        {
                            stat.CohortSize = size;
                            stat.ReturnedCustomers = returned;
                            stat.UpdatedAt = now;
                        }
                        else
                        {
                            db.CustomerRetentionStats.Add(
                                new CustomerRetentionStat
                                {
                                    CohortMonth = cohort,
                                    MonthOffset = offset,
                                    CohortSize = size,
                                    ReturnedCustomers = returned,
                                    UpdatedAt = now,
                                }
                            );
                        }
                    }
                }

                await db.SaveChangesAsync(cancellationToken);
            },
            cancellationToken
        );

    private async Task<DateOnly?> ResolveFromMonthAsync(
        DateOnly currentMonth,
        CancellationToken cancellationToken
    )
    {
        if (await db.CustomerRetentionStats.AnyAsync(cancellationToken))
            return currentMonth.AddMonths(-RetentionRules.MaxMonthOffset);

        var first = await db
            .Users.AsNoTracking()
            .Where(u => u.Role.RoleName == Roles.Customer)
            .MinAsync(u => (DateTime?)u.CreatedAt, cancellationToken);
        return first is { } createdAt ? RetentionRules.MonthOf(createdAt) : null;
    }
}
