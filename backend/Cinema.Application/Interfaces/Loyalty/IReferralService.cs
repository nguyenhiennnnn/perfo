using Cinema.Application.DTOs.Loyalty;
using Cinema.Domain.Entities;

namespace Cinema.Application.Interfaces.Loyalty;

public interface IReferralService
{
    Task AttachAsync(User referred, string referralCode, CancellationToken cancellationToken);

    Task<MyReferralsDto> GetMineAsync(int userId, CancellationToken cancellationToken);
}
