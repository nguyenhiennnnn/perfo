using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public class BundleReservation
{
    public long BundleReservationId { get; set; }
    public int OrderId { get; set; }
    public int BundleId { get; set; }
    public DateOnly BusinessDate { get; set; }
    public int Quantity { get; set; }
    public ResourceHoldStatus Status { get; set; } = ResourceHoldStatus.Reserved;
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }

    public Order Order { get; set; } = null!;
    public Bundle Bundle { get; set; } = null!;
}
