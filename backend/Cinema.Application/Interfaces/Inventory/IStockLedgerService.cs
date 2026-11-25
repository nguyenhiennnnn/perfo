using Cinema.Application.DTOs.Inventory;

namespace Cinema.Application.Interfaces.Inventory;

public interface IStockLedgerService
{
    Task<StockPosting> PostAsync(StockMovement movement, CancellationToken cancellationToken);
}
