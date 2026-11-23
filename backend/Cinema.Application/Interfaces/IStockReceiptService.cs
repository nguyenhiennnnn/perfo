using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Inventory;

namespace Cinema.Application.Interfaces;

public interface IStockReceiptService
{
    Task<OffsetPageResult<StockReceiptListItemDto>> GetAdminAsync(
        int page,
        int pageSize,
        StockReceiptListFilter filter,
        CancellationToken cancellationToken
    );

    Task<StockReceiptDto> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<StockReceiptDto> CreateAsync(
        CreateStockReceiptRequest request,
        CancellationToken cancellationToken
    );
}
