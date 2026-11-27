using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public class ShowtimeEventGiftReservation
{
    public int ShowtimeEventGiftReservationId { get; set; }
    public int OrderId { get; set; }
    public int OrderLineId { get; set; }
    public int ShowtimeId { get; set; }
    public int InventoryItemId { get; set; }
    public int Quantity { get; set; }
    public int QuantityGiven { get; set; }
    public int QuantityReturned { get; set; }
    public ResourceHoldStatus Status { get; set; } = ResourceHoldStatus.Reserved;
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }

    public Order Order { get; set; } = null!;
    public OrderLine OrderLine { get; set; } = null!;
    public InventoryItem InventoryItem { get; set; } = null!;
}
