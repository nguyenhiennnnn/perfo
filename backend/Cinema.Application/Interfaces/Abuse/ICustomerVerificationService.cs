using Cinema.Application.DTOs.Abuse;

namespace Cinema.Application.Interfaces.Abuse;

public interface ICustomerVerificationService
{
    Task EnsureVerifiedAsync(int userId, CancellationToken cancellationToken);

    Task<CustomerVerificationStatusDto> GetStatusAsync(
        int userId,
        CancellationToken cancellationToken
    );

    Task<CustomerVerificationStatusDto> SendCodeAsync(
        int userId,
        CancellationToken cancellationToken
    );

    Task<CustomerVerificationStatusDto> ConfirmAsync(
        int userId,
        string code,
        CancellationToken cancellationToken
    );
}
