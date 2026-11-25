namespace Cinema.Application.Interfaces.Users;

public interface IReferralCodeService
{
    Task<string> GenerateUniqueAsync(CancellationToken cancellationToken);
}
