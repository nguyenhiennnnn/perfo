using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Inventory;

namespace Cinema.Application.Interfaces;

public interface IComboRecipeService
{
    Task<OffsetPageResult<ComboRecipeListItemDto>> GetAdminAsync(
        int page,
        int pageSize,
        ComboRecipeListFilter filter,
        CancellationToken cancellationToken
    );

    Task<ComboRecipeDto> GetByComboIdAsync(int comboId, CancellationToken cancellationToken);

    Task<ComboRecipeDto> UpsertAsync(
        int comboId,
        UpsertComboRecipeRequest request,
        CancellationToken cancellationToken
    );

    Task<DeleteComboRecipeResponse> DeleteAsync(int comboId, CancellationToken cancellationToken);
}
