using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Inventory;

namespace Cinema.Application.Interfaces.Inventory;

public interface IInventoryItemService
{
    Task<OffsetPageResult<InventoryItemDto>> GetAdminAsync(
        int page,
        int pageSize,
        InventoryItemListFilter filter,
        CancellationToken cancellationToken
    );

    Task<InventoryItemDto> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<InventoryItemDto> CreateAsync(
        UpsertInventoryItemRequest request,
        CancellationToken cancellationToken
    );

    Task<InventoryItemDto> UpdateAsync(
        int id,
        UpsertInventoryItemRequest request,
        CancellationToken cancellationToken
    );

    Task<DeleteInventoryItemResponse> DeleteAsync(int id, CancellationToken cancellationToken);
}
