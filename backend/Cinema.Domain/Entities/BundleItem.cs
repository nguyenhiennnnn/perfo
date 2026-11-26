namespace Cinema.Domain.Entities;

public class BundleItem
{
    public int BundleItemId { get; set; }
    public int BundleId { get; set; }
    public int? TicketTypeId { get; set; }
    public int? ComboId { get; set; }
    public int Quantity { get; set; }

    public Bundle Bundle { get; set; } = null!;
    public TicketType? TicketType { get; set; }
    public Combo? Combo { get; set; }
}
