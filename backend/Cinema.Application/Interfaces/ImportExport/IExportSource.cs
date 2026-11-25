using Cinema.Application.DTOs.Common;
using Cinema.Application.Utils;

namespace Cinema.Application.Interfaces.ImportExport;

public interface IExportSource<TFilter>
{
    string Module { get; }

    Task<int> CountAsync(TFilter filter, CancellationToken cancellationToken);

    Task<ExportFileDto> ExportAsync(
        TFilter filter,
        ExportFormat format,
        ExportContext context,
        IJobProgressReporter? progress,
        CancellationToken cancellationToken
    );
}

public interface IExportModule
{
    string Module { get; }

    Task<int> CountAsync(string filterJson, CancellationToken cancellationToken);

    Task<ExportFileDto> ExportAsync(
        string filterJson,
        ExportFormat format,
        ExportContext context,
        IJobProgressReporter? progress,
        CancellationToken cancellationToken
    );
}
