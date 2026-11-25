using Cinema.Application.DTOs.Support;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Services.Promotions;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Support;

public static class SupportApologyPromotionFactory
{
    private const string CodePrefix = "XL";

    public static Promotion Build(
        string code,
        string name,
        IssueSupportApologyCodeRequest request,
        int userId,
        DateTime now,
        int validDays
    ) =>
        new()
        {
            Code = code,
            Name = name,
            Type = PromotionType.PersonalCode,
            DiscountType = request.DiscountType,
            DiscountValue = request.DiscountValue,
            MaxDiscountAmount =
                request.DiscountType == PromotionDiscountType.Percent
                    ? request.MaxDiscountAmount
                    : null,
            Scope = PromotionScope.WholeOrder,
            StartsAt = now,
            EndsAt = now.AddDays(validDays),
            TotalUsageLimit = 1,
            PerCustomerLimit = 1,
            AssignedUserId = userId,
            IsActive = true,
            CreatedAt = now,
        };

    public static async Task<string> NewCodeAsync(
        IAppDbContext db,
        CancellationToken cancellationToken
    )
    {
        for (var attempt = 0; attempt < PromotionLimits.GenerateCodeMaxAttempts; attempt++)
        {
            var code = $"{CodePrefix}-{ReferralCodeGenerator.Generate()}";
            if (!await db.Promotions.AnyAsync(p => p.Code == code, cancellationToken))
                return code;
        }

        throw new ConflictAppException(Messages.Conflict_PromotionCodeExists);
    }
}
