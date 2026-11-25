using Cinema.Domain.Entities;
using Cinema.Domain.Enums;

namespace Cinema.Application.Interfaces.Auth;

public interface IVerificationTokenSender
{
    Task IssueVerificationTokenAndSendEmailAsync(
        User user,
        VerificationTokenPurpose purpose,
        TimeSpan validFor,
        CancellationToken cancellationToken
    );
}
