namespace Cinema.Application.Interfaces.Inventory;

public interface ILowStockAlertService
{
    Task<int> RunAsync(CancellationToken cancellationToken);
}
