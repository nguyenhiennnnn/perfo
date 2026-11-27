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

public class ShowtimeEventLimitService(IAppDbContext db, IAuditLogger auditLogger)
    : IShowtimeEventLimitService
{
    public async Task<ShowtimeEventLimitsDto> GetAsync(
        int showtimeId,
        CancellationToken cancellationToken
    )
    {
        var setting =
            await db
                .ShowtimeEventSettings.AsNoTracking()
                .Include(s => s.TicketQuotas)
                .ThenInclude(q => q.TicketType)
                .FirstOrDefaultAsync(s => s.ShowtimeId == showtimeId, cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_ShowtimeEvent);
        return await ToDtoAsync(setting, cancellationToken);
    }

    public async Task<ShowtimeEventLimitsDto> UpsertAsync(
        int showtimeId,
        UpsertShowtimeEventLimitsRequest request,
        CancellationToken cancellationToken
    )
    {
        var quotas = request.TicketQuotas ?? [];
        var ticketTypeIds = quotas.Select(q => q.TicketTypeId).ToList();
        var names = await db
            .TicketTypes.AsNoTracking()
            .Where(t => ticketTypeIds.Contains(t.TicketTypeId))
            .ToDictionaryAsync(t => t.TicketTypeId, t => t.Name, cancellationToken);
        if (names.Count != ticketTypeIds.Count)
            throw new NotFoundException(Messages.NotFound_TicketType);

        ShowtimeEventLimitsDto? result = null;
        await db.InTransactionAsync(
            async () =>
            {
                await LockShowtimeAsync(showtimeId, cancellationToken);

                var showtime =
                    await db
                        .Showtimes.AsNoTracking()
                        .Where(s => s.ShowtimeId == showtimeId)
                        .Select(s => new { s.Status, s.StartTime })
                        .FirstOrDefaultAsync(cancellationToken)
                    ?? throw new NotFoundException(Messages.NotFound_Showtime);
                var setting =
                    await db
                        .ShowtimeEventSettings.Include(s => s.TicketQuotas)
                        .FirstOrDefaultAsync(s => s.ShowtimeId == showtimeId, cancellationToken)
                    ?? throw new NotFoundException(Messages.NotFound_ShowtimeEvent);
                if (
                    showtime.Status != ShowtimeStatus.Scheduled
                    || showtime.StartTime <= DateTime.UtcNow
                )
                    throw new BusinessRuleException(Messages.Rule_ShowtimeEventLimitStatusInvalid);

                var before = await ToDtoAsync(setting, cancellationToken);
                var now = DateTime.UtcNow;
                foreach (var quota in quotas)
                {
                    var used = await UsedAsync(showtimeId, quota.TicketTypeId, now, cancellationToken);
                    if (quota.MaxTickets < used)
                        throw new BusinessRuleException(
                            string.Format(
                                Messages.Rule_EventLimitQuotaBelowUsed,
                                names[quota.TicketTypeId],
                                used
                            )
                        );
                }

                setting.MaxTicketsPerCustomer = request.MaxTicketsPerCustomer;
                setting.UpdatedAt = now;
                var requested = quotas.ToDictionary(q => q.TicketTypeId, q => q.MaxTickets);
                foreach (var existing in setting.TicketQuotas.ToList())
                {
                    if (requested.TryGetValue(existing.TicketTypeId, out var max))
                        existing.MaxTickets = max;
                    else
                        db.ShowtimeEventTicketQuotas.Remove(existing);
                }

                var current = setting.TicketQuotas.Select(q => q.TicketTypeId).ToHashSet();
                foreach (var quota in quotas.Where(q => !current.Contains(q.TicketTypeId)))
                    db.ShowtimeEventTicketQuotas.Add(
                        new ShowtimeEventTicketQuota
                        {
                            ShowtimeId = showtimeId,
                            TicketTypeId = quota.TicketTypeId,
                            MaxTickets = quota.MaxTickets,
                        }
                    );

                await db.SaveChangesAsync(cancellationToken);

                var saved = await db
                    .ShowtimeEventSettings.AsNoTracking()
                    .Include(s => s.TicketQuotas)
                    .ThenInclude(q => q.TicketType)
                    .FirstAsync(s => s.ShowtimeId == showtimeId, cancellationToken);
                result = await ToDtoAsync(saved, cancellationToken);
                auditLogger.Record(
                    AuditActions.Update,
                    AuditEntityTypes.ShowtimeEvent,
                    showtimeId.ToString(),
                    before,
                    result
                );
                await db.SaveChangesAsync(cancellationToken);
            },
            cancellationToken
        );

        return result!;
    }

    public async Task EnsureWithinLimitsAsync(Order order, CancellationToken cancellationToken)
    {
        var groups = order
            .Lines.Where(l => l.LineType == OrderLineType.Ticket && l.ShowtimeId != null)
            .GroupBy(l => l.ShowtimeId!.Value)
            .OrderBy(g => g.Key)
            .ToList();
        foreach (var group in groups)
        {
            var limits = await db
                .ShowtimeEventSettings.AsNoTracking()
                .Where(s => s.ShowtimeId == group.Key)
                .Select(s => new
                {
                    s.MaxTicketsPerCustomer,
                    Quotas = s
                        .TicketQuotas.Select(q => new
                        {
                            q.TicketTypeId,
                            q.MaxTickets,
                            q.TicketType.Name,
                        })
                        .ToList(),
                })
                .FirstOrDefaultAsync(cancellationToken);
            if (limits is null || (limits.MaxTicketsPerCustomer is null && limits.Quotas.Count == 0))
                continue;

            await LockShowtimeAsync(group.Key, cancellationToken);
            var now = DateTime.UtcNow;

            foreach (var quota in limits.Quotas)
            {
                var requested = group
                    .Where(l => l.TicketTypeId == quota.TicketTypeId)
                    .Sum(l => l.Quantity);
                if (requested == 0)
                    continue;

                var used = await UsedAsync(group.Key, quota.TicketTypeId, now, cancellationToken);
                if (used > quota.MaxTickets)
                    throw new BusinessRuleException(
                        string.Format(
                            Messages.Rule_EventTicketTypeQuotaExceeded,
                            quota.Name,
                            Math.Max(0, quota.MaxTickets - (used - requested))
                        ),
                        ApiErrorCodes.EventTicketTypeQuotaExceeded
                    );
            }

            if (limits.MaxTicketsPerCustomer is not { } perCustomer)
                continue;

            var mine = db.ActiveTickets(group.Key, now);
            if (order.UserId is { } userId)
                mine = mine.Where(l => l.Order.UserId == userId);
            else if (!string.IsNullOrWhiteSpace(order.CustomerPhone))
            {
                var phone = order.CustomerPhone;
                mine = mine.Where(l => l.Order.UserId == null && l.Order.CustomerPhone == phone);
            }
            else
                continue;

            var total = await mine.SumAsync(l => l.Quantity - l.RefundedQuantity, cancellationToken);
            if (total > perCustomer)
            {
                var requested = group.Sum(l => l.Quantity);
                throw new BusinessRuleException(
                    string.Format(
                        Messages.Rule_EventCustomerLimitExceeded,
                        perCustomer,
                        Math.Max(0, total - requested)
                    ),
                    ApiErrorCodes.EventCustomerLimitExceeded
                );
            }
        }
    }

    private Task LockShowtimeAsync(int showtimeId, CancellationToken cancellationToken) =>
        db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({ShowtimeEventLimitRules.LockClassId}, {showtimeId})",
            cancellationToken
        );

    private Task<int> UsedAsync(
        int showtimeId,
        int ticketTypeId,
        DateTime now,
        CancellationToken cancellationToken
    ) =>
        db.ActiveTickets(showtimeId, now)
            .Where(l => l.TicketTypeId == ticketTypeId)
            .SumAsync(l => l.Quantity - l.RefundedQuantity, cancellationToken);

    private async Task<ShowtimeEventLimitsDto> ToDtoAsync(
        ShowtimeEventSetting setting,
        CancellationToken cancellationToken
    )
    {
        var now = DateTime.UtcNow;
        var quotas = new List<ShowtimeEventTicketQuotaDto>(setting.TicketQuotas.Count);
        foreach (var quota in setting.TicketQuotas.OrderBy(q => q.TicketTypeId))
        {
            var name =
                quota.TicketType?.Name
                ?? await db
                    .TicketTypes.AsNoTracking()
                    .Where(t => t.TicketTypeId == quota.TicketTypeId)
                    .Select(t => t.Name)
                    .FirstAsync(cancellationToken);
            quotas.Add(
                new ShowtimeEventTicketQuotaDto(
                    quota.TicketTypeId,
                    name,
                    quota.MaxTickets,
                    await UsedAsync(setting.ShowtimeId, quota.TicketTypeId, now, cancellationToken)
                )
            );
        }

        return new ShowtimeEventLimitsDto(setting.ShowtimeId, setting.MaxTicketsPerCustomer, quotas);
    }
}
