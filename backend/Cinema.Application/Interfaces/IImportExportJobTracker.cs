using Cinema.Application.DTOs.Common;
using Cinema.Domain.Enums;

namespace Cinema.Application.Interfaces;

public sealed record ImportExportJobRunContext(int UserId, int TotalRows, string? ActorRole);

public interface IImportExportJobTracker
{
    Task<Guid> CreateQueuedAsync(
        ImportExportJobType type,
        string module,
        string format,
        ActorContext actor,
        string? fileName,
        int totalRows,
        string? parameters,
        CancellationToken cancellationToken
    );

    Task<ImportExportJobRunContext?> MarkRunningAsync(
        Guid jobId,
        CancellationToken cancellationToken
    );

    Task ReportProgressAsync(Guid jobId, int progress, CancellationToken cancellationToken);

    Task CompleteExportAsync(
        Guid jobId,
        string fileName,
        FileUploadResult file,
        long? fileSizeBytes,
        int totalRows,
        int retentionHours,
        CancellationToken cancellationToken
    );

    Task CompleteImportAsync(
        Guid jobId,
        int successRows,
        int failedRows,
        string? errorMessage,
        CancellationToken cancellationToken
    );

    Task FailAsync(Guid jobId, string errorMessage, CancellationToken cancellationToken);

    Task RecordCompletedAsync(
        ImportExportJobType type,
        string module,
        string format,
        ActorContext actor,
        string? fileName,
        int totalRows,
        int successRows,
        int failedRows,
        string? parameters,
        long? fileSizeBytes,
        CancellationToken cancellationToken
    );

    Task RecordFailedAsync(
        ImportExportJobType type,
        string module,
        string format,
        ActorContext actor,
        string? fileName,
        string? parameters,
        string errorMessage,
        CancellationToken cancellationToken
    );
}
