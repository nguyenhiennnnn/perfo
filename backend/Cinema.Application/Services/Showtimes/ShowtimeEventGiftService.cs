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

public class ShowtimeEventGiftService(IAppDbContext db, IAuditLogger auditLogger)
    : IShowtimeEventGiftService
{
    public async Task<ShowtimeEventGiftsDto> GetAsync(
        int showtimeId,
        CancellationToken cancellationToken
    )
    {
        var exists = await db.ShowtimeEventSettings.AnyAsync(
            s => s.ShowtimeId == showtimeId,
            cancellationToken
        );
        if (!exists)
            throw new NotFoundException(Messages.NotFound_ShowtimeEvent);
        return await BuildAsync(showtimeId, cancellationToken);
    }

    public async Task<ShowtimeEventGiftsDto> UpsertAsync(
        int showtimeId,
        UpsertShowtimeEventGiftsRequest request,
        CancellationToken cancellationToken
    )
    {
        var requested = request.Gifts ?? [];
        var itemIds = requested.Select(g => g.InventoryItemId).ToList();
        var items = await db
            .InventoryItems.AsNoTracking()
            .Where(i => itemIds.Contains(i.InventoryItemId) && i.IsActive)
            .ToDictionaryAsync(i => i.InventoryItemId, i => i.Name, cancellationToken);
        if (items.Count != itemIds.Count)
            throw new NotFoundException(Messages.NotFound_InventoryItem);

        ShowtimeEventGiftsDto? result = null;
        await db.InTransactionAsync(
            async () =>
            {
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT pg_advisory_xact_lock({ShowtimeEventLimitRules.LockClassId}, {showtimeId})",
                    cancellationToken
                );

                var showtime =
                    await db
                        .Showtimes.AsNoTracking()
                        .Where(s => s.ShowtimeId == showtimeId)
                        .Select(s => new { s.Status, s.StartTime })
                        .FirstOrDefaultAsync(cancellationToken)
                    ?? throw new NotFoundException(Messages.NotFound_Showtime);
                var setting =
                    await db
                        .ShowtimeEventSettings.Include(s => s.Gifts)
                        .FirstOrDefaultAsync(s => s.ShowtimeId == showtimeId, cancellationToken)
                    ?? throw new NotFoundException(Messages.NotFound_ShowtimeEvent);
                if (
                    showtime.Status != ShowtimeStatus.Scheduled
                    || showtime.StartTime <= DateTime.UtcNow
                )
                    throw new BusinessRuleException(Messages.Rule_ShowtimeEventGiftStatusInvalid);

                var before = await BuildAsync(showtimeId, cancellationToken);

                var byItem = requested.ToDictionary(g => g.InventoryItemId);
                foreach (var existing in setting.Gifts.ToList())
                {
                    var held = await HeldAsync(showtimeId, existing.InventoryItemId, cancellationToken);
                    if (!byItem.TryGetValue(existing.InventoryItemId, out var next))
                    {
                        if (held > 0)
                            throw await BelowHeldAsync(existing.InventoryItemId, held, cancellationToken);
                        db.ShowtimeEventGifts.Remove(existing);
                        continue;
                    }

                    if (next.Quantity < held)
                        throw await BelowHeldAsync(existing.InventoryItemId, held, cancellationToken);
                    if (next.QuantityPerTicket != existing.QuantityPerTicket && held > 0)
                        throw new BusinessRuleException(Messages.Rule_ShowtimeEventGiftPerTicketLocked);

                    existing.Quantity = next.Quantity;
                    existing.QuantityPerTicket = next.QuantityPerTicket;
                }

                var current = setting.Gifts.Select(g => g.InventoryItemId).ToHashSet();
                foreach (var gift in requested.Where(g => !current.Contains(g.InventoryItemId)))
                    db.ShowtimeEventGifts.Add(
                        new ShowtimeEventGift
                        {
                            ShowtimeId = showtimeId,
                            InventoryItemId = gift.InventoryItemId,
                            Quantity = gift.Quantity,
                            QuantityPerTicket = gift.QuantityPerTicket,
                        }
                    );

                setting.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellationToken);

                result = await BuildAsync(showtimeId, cancellationToken);
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

    private async Task<BusinessRuleException> BelowHeldAsync(
        int itemId,
        int held,
        CancellationToken cancellationToken
    )
    {
        var name = await db
            .InventoryItems.IgnoreQueryFilters()
            .Where(i => i.InventoryItemId == itemId)
            .Select(i => i.Name)
            .FirstAsync(cancellationToken);
        return new BusinessRuleException(
            string.Format(Messages.Rule_ShowtimeEventGiftBelowHeld, name, held)
        );
    }

    private Task<int> HeldAsync(int showtimeId, int itemId, CancellationToken cancellationToken) =>
        db.Held(showtimeId, itemId).SumAsync(r => r.Quantity - r.QuantityReturned, cancellationToken);

    private async Task<ShowtimeEventGiftsDto> BuildAsync(
        int showtimeId,
        CancellationToken cancellationToken
    )
    {
        var gifts = await db
            .ShowtimeEventGifts.AsNoTracking()
            .Where(g => g.ShowtimeId == showtimeId)
            .OrderBy(g => g.InventoryItemId)
            .Select(g => new
            {
                g.InventoryItemId,
                g.InventoryItem.Code,
                g.InventoryItem.Name,
                g.InventoryItem.Unit,
                g.Quantity,
                g.QuantityPerTicket,
            })
            .ToListAsync(cancellationToken);

        var counts = await db
            .ShowtimeEventGiftReservations.AsNoTracking()
            .Where(r =>
                r.ShowtimeId == showtimeId
                && (r.Status == ResourceHoldStatus.Reserved || r.Status == ResourceHoldStatus.Committed)
            )
            .GroupBy(r => r.InventoryItemId)
            .Select(g => new
            {
                ItemId = g.Key,
                Held = g.Sum(r => r.Quantity - r.QuantityReturned),
                Given = g.Sum(r => r.QuantityGiven),
            })
            .ToDictionaryAsync(x => x.ItemId, cancellationToken);

        return new ShowtimeEventGiftsDto(
            showtimeId,
            [
                .. gifts.Select(g => new ShowtimeEventGiftDto(
                    g.InventoryItemId,
                    g.Code,
                    g.Name,
                    g.Unit,
                    g.Quantity,
                    g.QuantityPerTicket,
                    counts.TryGetValue(g.InventoryItemId, out var c) ? c.Held : 0,
                    c?.Given ?? 0
                )),
            ]
        );
    }
}
