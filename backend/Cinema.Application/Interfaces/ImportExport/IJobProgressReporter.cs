namespace Cinema.Application.Interfaces.ImportExport;

public interface IJobProgressReporter
{
    Task ReportAsync(int processedRows, CancellationToken cancellationToken);
}
