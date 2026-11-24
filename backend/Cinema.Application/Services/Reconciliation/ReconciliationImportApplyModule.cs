using Cinema.Application.DTOs.Common;
using Cinema.Application.Interfaces;
using Cinema.Shared.Constants;

namespace Cinema.Application.Services.Reconciliation;

public class ReconciliationImportApplyModule(IReconciliationImportServiceFactory factory)
    : IImportApplyModule
{
    public string Module => ImportExportModules.Reconciliation;

    public Task<BulkActionResponse> ApplyQueuedAsync(
        int userId,
        string previewId,
        bool skipInvalidRows,
        IJobProgressReporter progress,
        CancellationToken cancellationToken
    ) =>
        factory
            .CreateForSession()
            .RunQueuedApplyAsync(userId, previewId, skipInvalidRows, progress, cancellationToken);
}
