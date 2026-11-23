using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Promotions;

namespace Cinema.Application.Interfaces;

public interface IPromotionService
{
    Task<OffsetPageResult<PromotionDto>> GetAdminAsync(
        int page,
        int pageSize,
        PromotionListFilter filter,
        CancellationToken cancellationToken
    );

    Task<PromotionDto> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<PromotionDto> CreateAsync(
        UpsertPromotionRequest request,
        CancellationToken cancellationToken
    );

    Task<PromotionDto> UpdateAsync(
        int id,
        UpsertPromotionRequest request,
        CancellationToken cancellationToken
    );

    Task<PromotionDto> PauseAsync(int id, CancellationToken cancellationToken);

    Task<PromotionDto> ResumeAsync(int id, CancellationToken cancellationToken);

    Task<DeletePromotionResponse> DeleteAsync(int id, CancellationToken cancellationToken);
}
