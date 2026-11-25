namespace Cinema.Application.Interfaces.Audit;

public interface IAuditLogger
{
    void Record(string action, string entityType, string entityId, object? before, object? after);
}
