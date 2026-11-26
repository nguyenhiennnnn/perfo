using Cinema.Application.Interfaces.Abuse;
using Cinema.Application.Interfaces.Loyalty;

namespace Cinema.Application.Services.Loyalty;

public sealed class ReferralDeviceCheck(ICustomerIdentityLinkService identityLinks)
    : IReferralDeviceCheck
{
    public Task<bool> IsSuspiciousAsync(
        int referrerUserId,
        int referredUserId,
        CancellationToken cancellationToken
    ) => identityLinks.SharesDeviceWithOtherAsync(referredUserId, cancellationToken);
}
