using Cinema.Domain.Entities;

namespace Cinema.Application.Interfaces;

public interface IReconciliationAnomalyHook
{
    Task OnAnomalyAsync(ReconciliationItem item, CancellationToken cancellationToken);
}
