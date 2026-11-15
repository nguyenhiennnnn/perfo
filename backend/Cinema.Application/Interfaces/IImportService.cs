using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Import;
using Cinema.Application.Utils;

namespace Cinema.Application.Interfaces;

public interface IImportService<TRow>
{
    Task<ExportFileDto> GetTemplateAsync(CancellationToken cancellationToken);

    Task<ImportPreviewResponse> PreviewAsync(
        ActorContext actor,
        ImportFileInput file,
        CancellationToken cancellationToken
    );

    Task<ImportApplyOutcome> ApplyAsync(
        ActorContext actor,
        ImportApplyRequest request,
        CancellationToken cancellationToken
    );

    Task<BulkActionResponse> RunQueuedApplyAsync(
        int userId,
        string previewId,
        bool skipInvalidRows,
        IJobProgressReporter progress,
        CancellationToken cancellationToken
    );

    Task<ExportFileDto> GetErrorFileAsync(
        int userId,
        string previewId,
        CancellationToken cancellationToken
    );
}
