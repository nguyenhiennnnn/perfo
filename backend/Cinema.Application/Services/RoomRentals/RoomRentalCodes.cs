using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Utils;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.RoomRentals;

public static class RoomRentalCodes
{
    private const string Prefix = "RR";
    private const string Chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static async Task<string> GenerateUniqueAsync(
        IAppDbContext db,
        CancellationToken cancellationToken
    )
    {
        for (var attempt = 0; attempt < RoomRentalLimits.CodeGenerationMaxAttempts; attempt++)
        {
            var suffix = new string(
                Enumerable.Range(0, 5).Select(_ => Chars[Random.Shared.Next(Chars.Length)]).ToArray()
            );
            var code = $"{Prefix}{BusinessTime.Today():yyyyMMdd}{suffix}";
            if (!await db.RoomRentalRequests.AnyAsync(r => r.RequestCode == code, cancellationToken))
                return code;
        }

        throw new BusinessRuleException(Messages.Rule_RoomRentalCodeGenerationFailed);
    }
}
