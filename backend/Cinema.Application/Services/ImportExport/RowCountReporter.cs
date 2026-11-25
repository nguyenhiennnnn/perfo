using Cinema.Application.Interfaces.ImportExport;

namespace Cinema.Application.Services.ImportExport;

public sealed class RowCountReporter : IJobProgressReporter
{
    public int Count { get; private set; }

    public Task ReportAsync(int processedRows, CancellationToken cancellationToken)
    {
        Count = processedRows;
        return Task.CompletedTask;
    }
}
