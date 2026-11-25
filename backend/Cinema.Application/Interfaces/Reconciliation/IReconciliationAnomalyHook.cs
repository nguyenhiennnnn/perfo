using Cinema.Domain.Entities;

namespace Cinema.Application.Interfaces.Reconciliation;

public interface IReconciliationAnomalyHook
{
    Task OnAnomalyAsync(ReconciliationItem item, CancellationToken cancellationToken);
}
