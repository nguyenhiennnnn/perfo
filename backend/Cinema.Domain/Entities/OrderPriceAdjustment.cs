using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public class OrderPriceAdjustment
{
    public int AdjustmentId { get; set; }
    public int OrderId { get; set; }
    public PriceAdjustmentType Type { get; set; }
    public decimal Amount { get; set; }
    public string? ReferenceType { get; set; }
    public int? ReferenceId { get; set; }

    public Order Order { get; set; } = null!;
    public ICollection<OrderPriceAdjustmentAllocation> Allocations { get; set; } = [];
}
