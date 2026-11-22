using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public class Order
{
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = null!;
    public OrderType Type { get; set; } = OrderType.Ticket;
    public SalesChannel Channel { get; set; } = SalesChannel.Online;
    public int? UserId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int? CreatedByEmployeeId { get; set; }
    public int? CounterShiftId { get; set; }
    public int? OriginalOrderId { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public CancelReason? CancelReason { get; set; }
    public CancelledBy? CancelledBy { get; set; }
    public DateTime? CancelledAt { get; set; }
    public uint RowVersion { get; set; }

    public User? User { get; set; }
    public Employee? CreatedByEmployee { get; set; }
    public Order? OriginalOrder { get; set; }
    public ICollection<Order> SupplementOrders { get; set; } = [];
    public ICollection<OrderLine> Lines { get; set; } = [];
    public ICollection<OrderPriceAdjustment> PriceAdjustments { get; set; } = [];
    public ICollection<PaymentTransaction> PaymentTransactions { get; set; } = [];
    public ICollection<OrderResourceHold> ResourceHolds { get; set; } = [];
}
