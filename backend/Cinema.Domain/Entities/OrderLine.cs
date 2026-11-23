using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public class OrderLine
{
    public int OrderLineId { get; set; }
    public int OrderId { get; set; }
    public OrderLineType LineType { get; set; }
    public string? Description { get; set; }
    public int? ShowtimeId { get; set; }
    public int? ShowtimeSeatId { get; set; }
    public int? TicketTypeId { get; set; }
    public bool RequiresCardPresentation { get; set; }
    public CardCheckStatus CardCheckStatus { get; set; } = CardCheckStatus.NotRequired;
    public DateTime? CardCheckRecordedAt { get; set; }
    public int? CardCheckRecordedByUserId { get; set; }
    public int? SupplementedLineId { get; set; }
    public int? ComboId { get; set; }
    public int Quantity { get; set; } = 1;
    public int QuantityFulfilled { get; set; }
    public decimal? UnitCost { get; set; }
    public decimal UnitListPrice { get; set; }
    public decimal LockedUnitPrice { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RefundedAmount { get; set; }
    public int RefundedQuantity { get; set; }
    public string? QrCode { get; set; }
    public bool IsCheckedIn { get; set; }
    public DateTime? CheckedInAt { get; set; }

    public Order Order { get; set; } = null!;
    public Showtime? Showtime { get; set; }
    public ShowtimeSeat? ShowtimeSeat { get; set; }
    public TicketType? TicketType { get; set; }
    public OrderLine? SupplementedLine { get; set; }
    public Combo? Combo { get; set; }
    public ICollection<OrderPriceAdjustmentAllocation> AdjustmentAllocations { get; set; } = [];
}
