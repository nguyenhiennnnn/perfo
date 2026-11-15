namespace Cinema.Application.Interfaces;

public interface IImportExportConcurrencyGuard
{
    Task<IDisposable> AcquireAsync(int userId, CancellationToken cancellationToken);
}
