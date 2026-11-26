using System.Security.Cryptography;
using Cinema.Application.Caching;
using Cinema.Application.DTOs.Abuse;
using Cinema.Application.Interfaces.Abuse;
using Cinema.Application.Interfaces.Auth;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Notifications;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.Abuse;

public class CustomerVerificationService(
    IAppDbContext db,
    IAppCache cache,
    ITokenService tokenService,
    INotificationService notificationService,
    ILogger<CustomerVerificationService> logger
) : ICustomerVerificationService
{
    public const string RequiredErrorCode = "VERIFICATION_REQUIRED";
    public const string CooldownErrorCode = "VERIFICATION_CODE_COOLDOWN";
    public const string AttemptsExceededErrorCode = "VERIFICATION_ATTEMPTS_EXCEEDED";

    private const int ResendCooldownSeconds = 60;

    public async Task EnsureVerifiedAsync(int userId, CancellationToken cancellationToken)
    {
        var pending = await FindPendingAsync(userId, cancellationToken);
        if (pending is null)
            return;

        throw new ForbiddenAppException(
            Messages.Rule_VerificationRequired,
            RequiredErrorCode,
            new { reason = pending.Reason.ToString() }
        );
    }

    public async Task<CustomerVerificationStatusDto> GetStatusAsync(
        int userId,
        CancellationToken cancellationToken
    ) => await ToStatusAsync(await FindPendingAsync(userId, cancellationToken), cancellationToken);

    public async Task<CustomerVerificationStatusDto> SendCodeAsync(
        int userId,
        CancellationToken cancellationToken
    )
    {
        var pending =
            await db.CustomerVerifications.FirstOrDefaultAsync(
                v => v.UserId == userId && v.Status == CustomerVerificationStatus.Pending,
                cancellationToken
            ) ?? throw new BusinessRuleException(Messages.Rule_VerificationNotRequired);

        var now = DateTime.UtcNow;
        if (pending.CodeSentAt is { } sentAt && sentAt.AddSeconds(ResendCooldownSeconds) > now)
        {
            var wait = Math.Max(
                1,
                (int)Math.Ceiling((sentAt.AddSeconds(ResendCooldownSeconds) - now).TotalSeconds)
            );
            throw new TooManyAttemptsException(
                string.Format(Messages.Rule_VerificationCodeCooldown, wait),
                CooldownErrorCode,
                new { retryAfterSeconds = wait }
            );
        }

        var user = await db
            .Users.AsNoTracking()
            .Where(u => u.UserId == userId)
            .Select(u => new { u.Email, u.FullName })
            .FirstAsync(cancellationToken);

        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var validFor = TimeSpan.FromMinutes(settings.VerificationCodeMinutes);
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

        pending.CodeHash = HashCode(pending.CustomerVerificationId, code);
        pending.CodeSentAt = now;
        pending.CodeExpiresAt = now.Add(validFor);
        pending.FailedAttempts = 0;
        await db.SaveChangesAsync(cancellationToken);

        notificationService.SendVerificationCode(user.Email, user.FullName, code, validFor);
        logger.LogInformation(
            "VerificationCodeSent UserId {UserId} VerificationId {Id}",
            userId,
            pending.CustomerVerificationId
        );

        return await ToStatusAsync(pending, cancellationToken);
    }

    public async Task<CustomerVerificationStatusDto> ConfirmAsync(
        int userId,
        string code,
        CancellationToken cancellationToken
    )
    {
        var pending =
            await FindPendingAsync(userId, cancellationToken)
            ?? throw new BusinessRuleException(Messages.Rule_VerificationNotRequired);

        var now = DateTime.UtcNow;
        if (pending.CodeHash is null || pending.CodeExpiresAt is not { } expiresAt || expiresAt <= now)
            throw new BusinessRuleException(Messages.Rule_VerificationCodeNotSent);

        var id = pending.CustomerVerificationId;
        var hash = HashCode(id, code);
        var verified = await db
            .CustomerVerifications.Where(v =>
                v.CustomerVerificationId == id
                && v.Status == CustomerVerificationStatus.Pending
                && v.CodeHash == hash
                && v.CodeExpiresAt > now
            )
            .ExecuteUpdateAsync(
                setters =>
                    setters
                        .SetProperty(v => v.Status, CustomerVerificationStatus.Verified)
                        .SetProperty(v => v.VerifiedAt, (DateTime?)now)
                        .SetProperty(v => v.CodeHash, (string?)null)
                        .SetProperty(v => v.CodeExpiresAt, (DateTime?)null),
                cancellationToken
            );
        if (verified > 0)
        {
            logger.LogInformation("VerificationConfirmed UserId {UserId}", userId);
            return await ToStatusAsync(null, cancellationToken);
        }

        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        await db
            .CustomerVerifications.Where(v =>
                v.CustomerVerificationId == id
                && v.Status == CustomerVerificationStatus.Pending
                && v.CodeHash != null
            )
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(v => v.FailedAttempts, v => v.FailedAttempts + 1),
                cancellationToken
            );
        var failed = await db
            .CustomerVerifications.AsNoTracking()
            .Where(v => v.CustomerVerificationId == id)
            .Select(v => v.FailedAttempts)
            .FirstAsync(cancellationToken);

        var left = settings.VerificationMaxAttempts - failed;
        if (left > 0)
            throw new BusinessRuleException(
                string.Format(Messages.Rule_VerificationCodeInvalid, left)
            );

        await db
            .CustomerVerifications.Where(v =>
                v.CustomerVerificationId == id && v.Status == CustomerVerificationStatus.Pending
            )
            .ExecuteUpdateAsync(
                setters =>
                    setters
                        .SetProperty(v => v.CodeHash, (string?)null)
                        .SetProperty(v => v.CodeExpiresAt, (DateTime?)null)
                        .SetProperty(v => v.FailedAttempts, 0),
                cancellationToken
            );
        throw new TooManyAttemptsException(
            Messages.Rule_VerificationAttemptsExceeded,
            AttemptsExceededErrorCode
        );
    }

    private Task<CustomerVerification?> FindPendingAsync(
        int userId,
        CancellationToken cancellationToken
    ) =>
        db
            .CustomerVerifications.AsNoTracking()
            .FirstOrDefaultAsync(
                v => v.UserId == userId && v.Status == CustomerVerificationStatus.Pending,
                cancellationToken
            );

    private async Task<CustomerVerificationStatusDto> ToStatusAsync(
        CustomerVerification? pending,
        CancellationToken cancellationToken
    )
    {
        if (pending is null)
            return new CustomerVerificationStatusDto(false, null, null, null, null);

        var codeActive = pending.CodeHash is not null && pending.CodeExpiresAt > DateTime.UtcNow;
        int? attemptsLeft = null;
        if (codeActive)
        {
            var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
            attemptsLeft = Math.Max(0, settings.VerificationMaxAttempts - pending.FailedAttempts);
        }

        return new CustomerVerificationStatusDto(
            true,
            pending.Reason.ToString(),
            pending.CodeSentAt,
            codeActive ? pending.CodeExpiresAt : null,
            attemptsLeft
        );
    }

    private string HashCode(int verificationId, string code) =>
        tokenService.HashOpaqueToken($"{verificationId}:{code}");
}
