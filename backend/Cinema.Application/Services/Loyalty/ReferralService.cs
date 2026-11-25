using Cinema.Application.DTOs.Loyalty;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Loyalty;
using Cinema.Domain.Entities;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Loyalty;

public class ReferralService(IAppDbContext db) : IReferralService
{
    private const string FieldName = "referralCode";

    public async Task AttachAsync(
        User referred,
        string referralCode,
        CancellationToken cancellationToken
    )
    {
        var code = referralCode.Trim().ToUpperInvariant();
        var referrer = await db
            .CustomerProfiles.AsNoTracking()
            .Where(p => p.ReferralCode == code && p.User.IsActive)
            .Select(p => new { p.UserId, p.User.Email })
            .FirstOrDefaultAsync(cancellationToken);
        if (referrer is null)
            throw Invalid(Messages.Validation_ReferralCode_Invalid);

        if (
            (referred.UserId != 0 && referrer.UserId == referred.UserId)
            || string.Equals(referrer.Email, referred.Email, StringComparison.OrdinalIgnoreCase)
        )
            throw Invalid(Messages.Validation_ReferralCode_Self);

        if (
            referred.UserId != 0
            && await db.ReferralRelations.AnyAsync(
                r => r.ReferredUserId == referred.UserId,
                cancellationToken
            )
        )
            throw Invalid(Messages.Validation_ReferralCode_AlreadyReferred);

        db.ReferralRelations.Add(
            new ReferralRelation
            {
                ReferrerUserId = referrer.UserId,
                Referred = referred,
                CreatedAt = DateTime.UtcNow,
            }
        );
    }

    public async Task<MyReferralsDto> GetMineAsync(
        int userId,
        CancellationToken cancellationToken
    )
    {
        var code = await db
            .CustomerProfiles.AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => p.ReferralCode)
            .FirstOrDefaultAsync(cancellationToken);

        var rows = await db
            .ReferralRelations.AsNoTracking()
            .Where(r => r.ReferrerUserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.ReferralId)
            .Select(r => new
            {
                r.Referred.FullName,
                r.CreatedAt,
                r.RewardGrantedAt,
            })
            .ToListAsync(cancellationToken);

        var items = rows.Select(r => new ReferralItemDto(
                MaskName(r.FullName),
                r.CreatedAt,
                r.RewardGrantedAt is not null,
                r.RewardGrantedAt
            ))
            .ToList();
        return new MyReferralsDto(
            code,
            items.Count,
            items.Count(i => i.RewardGranted),
            items
        );
    }

    private static string MaskName(string fullName)
    {
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return string.Empty;
        return string.Join(' ', parts.Select((p, i) => i == 0 ? p : $"{p[0]}."));
    }

    private static ValidationAppException Invalid(string message) =>
        new(message, new Dictionary<string, string[]> { [FieldName] = [message] });
}
