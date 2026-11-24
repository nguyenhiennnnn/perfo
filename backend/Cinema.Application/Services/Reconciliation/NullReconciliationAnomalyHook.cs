using Cinema.Application.Interfaces;
using Cinema.Domain.Entities;

namespace Cinema.Application.Services.Reconciliation;

public sealed class NullReconciliationAnomalyHook : IReconciliationAnomalyHook
{
    public Task OnAnomalyAsync(ReconciliationItem item, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
