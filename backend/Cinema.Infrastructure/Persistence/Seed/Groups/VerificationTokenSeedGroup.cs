using System.Security.Cryptography;
using System.Text;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Infrastructure.Persistence.Seed.Groups;

internal sealed class VerificationTokenSeedGroup : ISeedGroup
{
    public string Name => "verification-tokens";

    public Task SeedAsync(SeedContext context, CancellationToken cancellationToken) =>
        SeedVerificationTokensAsync(context.Db, context.Logger, cancellationToken);

    internal const string DemoEmailConfirmToken = "demo-email-confirm-token";
    internal const string DemoPasswordResetToken = "demo-password-reset-token";
    internal const string DemoExpiredResetToken = "demo-expired-reset-token";
    internal const string DemoUsedConfirmToken = "demo-used-confirm-token";

    private static async Task SeedVerificationTokensAsync(
        AppDbContext db,
        ILogger logger,
        CancellationToken cancellationToken
    )
    {
        var now = DateTime.UtcNow;
        var tokens = new (
            string Email,
            string Raw,
            VerificationTokenPurpose Purpose,
            TimeSpan ExpiresIn,
            bool Used
        )[]
        {
            (
                SeedEmails.CustomerUnverified,
                DemoEmailConfirmToken,
                VerificationTokenPurpose.EmailConfirmation,
                TimeSpan.FromDays(30),
                false
            ),
            (
                SeedEmails.CustomerNew,
                DemoPasswordResetToken,
                VerificationTokenPurpose.PasswordReset,
                TimeSpan.FromDays(30),
                false
            ),
            (
                SeedEmails.Customer1,
                DemoExpiredResetToken,
                VerificationTokenPurpose.PasswordReset,
                TimeSpan.FromDays(-1),
                false
            ),
            (
                SeedEmails.Customer2,
                DemoUsedConfirmToken,
                VerificationTokenPurpose.EmailConfirmation,
                TimeSpan.FromDays(-3),
                true
            ),
        };

        var added = false;
        foreach (var t in tokens)
        {
            var hash = HashToken(t.Raw);
            if (
                await db.UserVerificationTokens.AnyAsync(
                    x => x.TokenHash == hash,
                    cancellationToken
                )
            )
                continue;

            var userId = await db
                .Users.Where(u => u.Email == t.Email)
                .Select(u => (int?)u.UserId)
                .FirstOrDefaultAsync(cancellationToken);
            if (userId is null)
                continue;

            db.UserVerificationTokens.Add(
                new UserVerificationToken
                {
                    UserId = userId.Value,
                    TokenHash = hash,
                    Purpose = t.Purpose,
                    ExpiresAt = now.Add(t.ExpiresIn),
                    Used = t.Used,
                    CreatedAt = t.ExpiresIn < TimeSpan.Zero ? now.AddDays(-4) : now,
                }
            );
            added = true;
        }

        if (added)
        {
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Seeded demo verification tokens: {Confirm} (email confirm), {Reset} (password reset)",
                DemoEmailConfirmToken,
                DemoPasswordResetToken
            );
        }
    }

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
