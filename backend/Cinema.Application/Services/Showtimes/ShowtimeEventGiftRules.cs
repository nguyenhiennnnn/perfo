using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;

namespace Cinema.Application.Services.Showtimes;

public static class ShowtimeEventGiftRules
{
    public const int MaxQuantity = 100_000;
    public const int MaxPerTicket = 10;

    public static IQueryable<ShowtimeEventGiftReservation> Held(
        this IAppDbContext db,
        int showtimeId,
        int inventoryItemId
    ) =>
        db.ShowtimeEventGiftReservations.Where(r =>
            r.ShowtimeId == showtimeId
            && r.InventoryItemId == inventoryItemId
            && (r.Status == ResourceHoldStatus.Reserved || r.Status == ResourceHoldStatus.Committed)
        );
}
