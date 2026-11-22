namespace Cinema.Domain.Entities;

public class OutboxMessage
{
    public long OutboxMessageId { get; set; }
    public string EventType { get; set; } = null!;
    public string Payload { get; set; } = null!;
    public DateTime OccurredAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<OutboxDelivery> Deliveries { get; set; } = [];
}
