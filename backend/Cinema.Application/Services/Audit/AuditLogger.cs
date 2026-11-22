using Cinema.Application.Interfaces;
using Cinema.Domain.Entities;

namespace Cinema.Application.Services.Audit;

public class AuditLogger(IAppDbContext db, ICurrentActorAccessor actorAccessor) : IAuditLogger
{
    public void Record(
        string action,
        string entityType,
        string entityId,
        object? before,
        object? after
    )
    {
        var beforeJson = before is null ? null : AuditJson.Serialize(before);
        var afterJson = after is null ? null : AuditJson.Serialize(after);
        if (beforeJson is not null && beforeJson == afterJson)
            return;

        db.AuditLogs.Add(
            new AuditLog
            {
                OccurredAt = DateTime.UtcNow,
                ActorUserId = actorAccessor.Current?.UserId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                BeforeJson = beforeJson,
                AfterJson = afterJson,
            }
        );
    }
}
