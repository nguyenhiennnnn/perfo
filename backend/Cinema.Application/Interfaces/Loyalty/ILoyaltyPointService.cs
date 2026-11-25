using Cinema.Application.DTOs.Orders;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;

namespace Cinema.Application.Interfaces.Loyalty;

public interface ILoyaltyPointService
{
    Task<int> GetBalanceAsync(int userId, CancellationToken cancellationToken);

    Task<LoyaltyStandingDto> GetStandingAsync(int userId, CancellationToken cancellationToken);

    Task<LoyaltyPointEntry> AddLotAsync(
        int userId,
        LoyaltyLotType type,
        int points,
        int? orderId,
        int? actorUserId,
        DateTime now,
        CancellationToken cancellationToken
    );

    Task SpendAsync(
        int userId,
        int points,
        LoyaltyTransactionType type,
        int? actorUserId,
        CancellationToken cancellationToken
    );

    Task RevokeAsync(
        int userId,
        int entryId,
        int orderId,
        int points,
        DateTime now,
        CancellationToken cancellationToken
    );

    Task<int> ExpireAsync(DateTime now, CancellationToken cancellationToken);
}
