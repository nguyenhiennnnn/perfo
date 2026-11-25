using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Notifications;
using Cinema.Application.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cinema.Application.Services.Loyalty;

public sealed record LoyaltyChallengeCompletion(
    string ChallengeName,
    int UserId,
    int? RewardPoints,
    string? RewardName,
    string? PromoCode,
    DateTime? CodeExpiresAtUtc
);

public class LoyaltyChallengeCompletionNotifier(
    IAppDbContext db,
    INotificationService notificationService,
    IOptions<AppUrlOptions> appUrlOptions,
    ILogger<LoyaltyChallengeCompletionNotifier> logger
)
{
    public async Task NotifyAsync(
        LoyaltyChallengeCompletion completion,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var recipient = await db
                .Users.AsNoTracking()
                .Where(u => u.UserId == completion.UserId)
                .Select(u => new { u.Email, u.FullName })
                .FirstOrDefaultAsync(cancellationToken);
            if (recipient is null || string.IsNullOrWhiteSpace(recipient.Email))
                return;

            notificationService.SendLoyaltyChallengeCompleted(
                recipient.Email,
                new LoyaltyChallengeCompletedEmailData(
                    recipient.FullName,
                    completion.ChallengeName,
                    completion.RewardPoints,
                    completion.RewardName,
                    completion.PromoCode,
                    completion.CodeExpiresAtUtc,
                    $"{appUrlOptions.Value.FrontendBaseUrl.TrimEnd('/')}/account"
                )
            );
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(
                ex,
                "LoyaltyChallengeNotificationFailed UserId {UserId}",
                completion.UserId
            );
        }
    }
}
