namespace Cinema.Domain.Entities;

public class OrderPriceAdjustmentAllocation
{
    public int AllocationId { get; set; }
    public int AdjustmentId { get; set; }
    public int OrderLineId { get; set; }
    public decimal Amount { get; set; }

    public OrderPriceAdjustment Adjustment { get; set; } = null!;
    public OrderLine OrderLine { get; set; } = null!;
}
