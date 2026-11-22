namespace Cinema.Application.DTOs.Audit;

public record AuditLogListFilter
{
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public int? ActorUserId { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}

public record AuditLogListItemDto(
    long AuditLogId,
    DateTime OccurredAt,
    int? ActorUserId,
    string? ActorName,
    string Action,
    string EntityType,
    string EntityId
);

public record AuditLogDetailDto(
    long AuditLogId,
    DateTime OccurredAt,
    int? ActorUserId,
    string? ActorName,
    string? ActorEmail,
    string Action,
    string EntityType,
    string EntityId,
    string? BeforeJson,
    string? AfterJson
);

public record AuditLogActorDto(int UserId, string FullName);

public record AuditLogFilterOptionsDto(
    List<string> EntityTypes,
    List<AuditLogActorDto> Actors
);
