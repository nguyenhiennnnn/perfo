using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Pricing;

namespace Cinema.Application.Interfaces;

public interface ISpecialDayService
{
    Task<OffsetPageResult<SpecialDayDto>> GetAdminAsync(
        int page,
        int pageSize,
        SpecialDayListFilter filter,
        CancellationToken cancellationToken
    );

    Task<SpecialDayDto> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<SpecialDayDto> CreateAsync(
        UpsertSpecialDayRequest request,
        CancellationToken cancellationToken
    );

    Task<SpecialDayDto> UpdateAsync(
        int id,
        UpsertSpecialDayRequest request,
        CancellationToken cancellationToken
    );

    Task<DeletePricingEntityResponse> DeleteAsync(int id, CancellationToken cancellationToken);
}
