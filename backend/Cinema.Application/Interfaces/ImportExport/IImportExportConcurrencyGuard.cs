namespace Cinema.Application.Interfaces.ImportExport;

public interface IImportExportConcurrencyGuard
{
    Task<IDisposable> AcquireAsync(int userId, CancellationToken cancellationToken);
}
