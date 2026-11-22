namespace Cinema.Application.Interfaces;

public interface IReferralCodeService
{
    Task<string> GenerateUniqueAsync(CancellationToken cancellationToken);
}
