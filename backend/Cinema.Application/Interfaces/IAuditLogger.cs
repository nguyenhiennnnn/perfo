namespace Cinema.Application.Interfaces;

public interface IAuditLogger
{
    void Record(string action, string entityType, string entityId, object? before, object? after);
}
