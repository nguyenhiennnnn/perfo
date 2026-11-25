using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Loyalty;

namespace Cinema.Application.Interfaces.Loyalty;

public interface ILoyaltyAdminService
{
    Task<IReadOnlyList<LoyaltyCustomerLookupItemDto>> SearchCustomersAsync(
        string? query,
        CancellationToken cancellationToken
    );

    Task<LoyaltyCustomerDetailDto> GetCustomerAsync(
        int userId,
        CancellationToken cancellationToken
    );

    Task<OffsetPageResult<LoyaltyTransactionDto>> GetTransactionsAsync(
        int userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    );

    Task<LoyaltyAdjustmentResultDto> AdjustAsync(
        int userId,
        int actorUserId,
        AdjustLoyaltyPointsRequest request,
        CancellationToken cancellationToken
    );
}
