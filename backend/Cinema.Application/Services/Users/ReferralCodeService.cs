using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Users;
using Cinema.Application.Utils;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Users;

public class ReferralCodeService(IAppDbContext db) : IReferralCodeService
{
    private const int MaxAttempts = 10;

    public async Task<string> GenerateUniqueAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var code = ReferralCodeGenerator.Generate();
            var taken = await db.CustomerProfiles.AnyAsync(
                p => p.ReferralCode == code,
                cancellationToken
            );
            if (!taken)
                return code;
        }

        throw new BusinessRuleException(Messages.Rule_ReferralCodeGenerationFailed);
    }
}
