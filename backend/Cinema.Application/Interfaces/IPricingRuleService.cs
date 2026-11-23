using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Pricing;

namespace Cinema.Application.Interfaces;

public interface IPricingRuleService
{
    Task<OffsetPageResult<PricingRuleDto>> GetAdminAsync(
        int page,
        int pageSize,
        PricingRuleListFilter filter,
        CancellationToken cancellationToken
    );

    Task<PricingRuleDto> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<PricingRuleDto> CreateAsync(
        UpsertPricingRuleRequest request,
        CancellationToken cancellationToken
    );

    Task<PricingRuleDto> UpdateAsync(
        int id,
        UpsertPricingRuleRequest request,
        CancellationToken cancellationToken
    );

    Task<DeletePricingEntityResponse> DeleteAsync(int id, CancellationToken cancellationToken);
}
