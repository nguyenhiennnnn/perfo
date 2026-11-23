using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Inventory;

namespace Cinema.Application.Interfaces;

public interface IWasteService
{
    Task<OffsetPageResult<WasteSheetListItemDto>> GetAdminAsync(
        int page,
        int pageSize,
        WasteSheetListFilter filter,
        CancellationToken cancellationToken
    );

    Task<WasteSheetDto> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<WasteSheetDto> CreateAsync(
        CreateWasteSheetRequest request,
        CancellationToken cancellationToken
    );
}
