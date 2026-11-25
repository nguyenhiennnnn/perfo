using Cinema.Application.DTOs.Common;
using Cinema.Application.Interfaces.ImportExport;


namespace Cinema.Application.Interfaces.Import;

public interface IImportApplyModule
{
    string Module { get; }

    Task<BulkActionResponse> ApplyQueuedAsync(
        int userId,
        string previewId,
        bool skipInvalidRows,
        IJobProgressReporter progress,
        CancellationToken cancellationToken
    );
}
