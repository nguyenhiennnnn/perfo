using Cinema.Application.Services.Promotions;

namespace Cinema.Application.Interfaces;

public interface IPromotionEngine
{
    Task<PromotionCheckResult> ValidateAsync(
        string code,
        PromotionContext context,
        CancellationToken cancellationToken
    );

    Task<PromotionResolution> ResolveAsync(
        PromotionContext context,
        string? code,
        bool includeAutoApply,
        CancellationToken cancellationToken
    );
}
