using Cinema.Application.Interfaces.Loyalty;

namespace Cinema.Application.Services.Loyalty;

public sealed class NullReferralDeviceCheck : IReferralDeviceCheck
{
    public Task<bool> IsSuspiciousAsync(
        int referrerUserId,
        int referredUserId,
        CancellationToken cancellationToken
    ) => Task.FromResult(false);
}
