using Cinema.Application.DTOs.Promotions;

namespace Cinema.Application.Interfaces;

public interface IPromotionBulkCodeService
{
    Task<BulkPersonalCodesResponse> CreateAsync(
        BulkPersonalCodesRequest request,
        CancellationToken cancellationToken
    );
}
