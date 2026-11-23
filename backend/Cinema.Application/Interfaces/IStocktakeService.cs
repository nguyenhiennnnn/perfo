using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Inventory;

namespace Cinema.Application.Interfaces;

public interface IStocktakeService
{
    Task<OffsetPageResult<StocktakeSheetListItemDto>> GetAdminAsync(
        int page,
        int pageSize,
        StocktakeSheetListFilter filter,
        CancellationToken cancellationToken
    );

    Task<StocktakeSheetDto> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<StocktakeSheetDto> CreateAsync(
        CreateStocktakeSheetRequest request,
        CancellationToken cancellationToken
    );
}
