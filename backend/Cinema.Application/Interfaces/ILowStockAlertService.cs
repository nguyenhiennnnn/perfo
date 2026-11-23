namespace Cinema.Application.Interfaces;

public interface ILowStockAlertService
{
    Task<int> RunAsync(CancellationToken cancellationToken);
}
