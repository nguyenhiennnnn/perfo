using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public class OrderResourceHold
{
    public int ResourceHoldId { get; set; }
    public int OrderId { get; set; }
    public string ResourceType { get; set; } = null!;
    public int? ResourceId { get; set; }
    public int Quantity { get; set; } = 1;
    public ResourceHoldStatus Status { get; set; } = ResourceHoldStatus.Reserved;
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }

    public Order Order { get; set; } = null!;
}
