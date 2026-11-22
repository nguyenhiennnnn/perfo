namespace Cinema.Domain.Entities;

public class AuditLog
{
    public long AuditLogId { get; set; }
    public DateTime OccurredAt { get; set; }
    public int? ActorUserId { get; set; }
    public string Action { get; set; } = null!;
    public string EntityType { get; set; } = null!;
    public string EntityId { get; set; } = null!;
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
}
