using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.ImportExport;
using Cinema.Application.Utils;

namespace Cinema.Application.Interfaces;

public interface IExportJobService
{
    Task<ExportFileDto> ExportNowAsync<TFilter>(
        ActorContext actor,
        string module,
        TFilter filter,
        ExportFormat format,
        CancellationToken cancellationToken
    );

    Task<ImportExportJobAcceptedDto> EnqueueAsync<TFilter>(
        ActorContext actor,
        string module,
        TFilter filter,
        ExportFormat format,
        CancellationToken cancellationToken
    );

    Task<ExportFileDto> RunTrackedAsync<TParameters>(
        ActorContext actor,
        string module,
        ExportFormat format,
        TParameters parameters,
        Func<ExportContext, IJobProgressReporter, Task<ExportFileDto>> export,
        CancellationToken cancellationToken
    );
}
