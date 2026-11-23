namespace Cinema.Domain.Entities;

public class ComboPickup
{
    public long ComboPickupId { get; set; }
    public int OrderId { get; set; }
    public int OrderLineId { get; set; }
    public int Quantity { get; set; }
    public int PickedUpByUserId { get; set; }
    public DateTime PickedUpAt { get; set; }

    public Order Order { get; set; } = null!;
    public OrderLine OrderLine { get; set; } = null!;
    public User PickedUpByUser { get; set; } = null!;
}
