using Cinema.Application.DTOs.Promotions;

namespace Cinema.Application.Interfaces.Promotions;

public interface IPromotionBulkCodeService
{
    Task<BulkPersonalCodesResponse> CreateAsync(
        BulkPersonalCodesRequest request,
        CancellationToken cancellationToken
    );
}
