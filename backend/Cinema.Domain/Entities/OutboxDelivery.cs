using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public class OutboxDelivery
{
    public long OutboxDeliveryId { get; set; }
    public long OutboxMessageId { get; set; }
    public string HandlerName { get; set; } = null!;
    public OutboxDeliveryStatus Status { get; set; } = OutboxDeliveryStatus.Pending;
    public int Attempts { get; set; }
    public DateTime NextAttemptAt { get; set; }
    public DateTime? LockedUntil { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAt { get; set; }

    public OutboxMessage Message { get; set; } = null!;
}
