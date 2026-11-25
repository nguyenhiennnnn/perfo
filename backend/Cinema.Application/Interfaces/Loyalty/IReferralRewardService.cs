namespace Cinema.Application.Interfaces.Loyalty;

public interface IReferralRewardService
{
    Task RewardForOrderAsync(int orderId, CancellationToken cancellationToken);
}

public interface IReferralDeviceCheck
{
    Task<bool> IsSuspiciousAsync(
        int referrerUserId,
        int referredUserId,
        CancellationToken cancellationToken
    );
}
