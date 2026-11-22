using Cinema.Application.Interfaces;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Utils;

public static class OrderCodes
{
    public static async Task<string> GenerateUniqueAsync(
        IAppDbContext db,
        int maxAttempts,
        CancellationToken cancellationToken
    )
    {
        for (var attempt = 0; attempt < Math.Max(1, maxAttempts); attempt++)
        {
            var code = CodeGenerator.GenerateBookingCode();
            if (!await db.Orders.AnyAsync(o => o.OrderCode == code, cancellationToken))
                return code;
        }

        throw new BusinessRuleException(Messages.Rule_BookingCodeGenerationFailed);
    }
}
