namespace Cinema.Domain.Entities;

public class ShowtimeEventGift
{
    public int ShowtimeId { get; set; }
    public int InventoryItemId { get; set; }
    public int Quantity { get; set; }
    public int QuantityPerTicket { get; set; } = 1;

    public ShowtimeEventSetting EventSetting { get; set; } = null!;
    public InventoryItem InventoryItem { get; set; } = null!;
}
