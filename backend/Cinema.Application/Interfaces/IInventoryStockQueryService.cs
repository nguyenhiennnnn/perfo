using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Inventory;

namespace Cinema.Application.Interfaces;

public interface IInventoryStockQueryService
{
    Task<OffsetPageResult<StockLevelDto>> GetStockLevelsAsync(
        int page,
        int pageSize,
        StockLevelFilter filter,
        CancellationToken cancellationToken
    );

    Task<StockLedgerResponse> GetLedgerAsync(
        int inventoryItemId,
        DateOnly? fromDate,
        DateOnly? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    );

    Task<InventoryValuationDto> GetValuationAsync(CancellationToken cancellationToken);
}
