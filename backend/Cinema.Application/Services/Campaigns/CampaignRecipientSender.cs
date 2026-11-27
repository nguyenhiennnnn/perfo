using Cinema.Application.Interfaces.Auth;
using Cinema.Application.Interfaces.Campaigns;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Notifications;
using Cinema.Application.Options;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cinema.Application.Services.Campaigns;

public class CampaignRecipientSender(
    IAppDbContext db,
    INotificationService notificationService,
    IUnsubscribeTokenService unsubscribeTokens,
    IOptions<BackgroundJobOptions> jobOptions,
    ILogger<CampaignRecipientSender> logger
) : ICampaignRecipientSender
{
    private const string SkipInactiveUser = "InactiveUser";
    private const string SkipNoConsent = "NoConsent";
    private const string SkipNoEmail = "NoEmail";

    public async Task<CampaignSendOutcome> SendAsync(
        Campaign campaign,
        CampaignRecipientTarget target,
        CancellationToken cancellationToken
    )
    {
        var options = jobOptions.Value;
        var attempt = target.AttemptCount + 1;

        if (!await ClaimAsync(campaign.CampaignId, target, attempt, options, cancellationToken))
            return CampaignSendOutcome.Busy;

        try
        {
            if (FindSkipReason(campaign, target) is { } reason)
                return await TrySkipAsync(campaign.CampaignId, target.UserId, attempt, reason)
                    ? CampaignSendOutcome.Skipped
                    : CampaignSendOutcome.Busy;

            var promo = campaign.PromoDiscountType is null
                ? null
                : await IssueCodeAsync(campaign, target, attempt, cancellationToken);
            var unsubscribeLink =
                campaign.Channel == CampaignChannel.Email
                    ? unsubscribeTokens.CreateUnsubscribeUrl(target.UserId)
                    : null;

            if (!await TryMarkSentAsync(campaign.CampaignId, target.UserId, attempt))
                return CampaignSendOutcome.Busy;

            if (campaign.Channel == CampaignChannel.Email)
                notificationService.SendCampaign(
                    target.Email!,
                    new CampaignEmailData(
                        target.FullName,
                        campaign.Subject,
                        campaign.Body,
                        promo,
                        unsubscribeLink,
                        false
                    )
                );

            return CampaignSendOutcome.Sent;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(
                ex,
                "CampaignRecipientFailed CampaignId {CampaignId} UserId {UserId} Attempt {Attempt}",
                campaign.CampaignId,
                target.UserId,
                attempt
            );
            await TryFailAsync(campaign.CampaignId, target.UserId, attempt, ex, options);
            return CampaignSendOutcome.Failed;
        }
    }

    private static string? FindSkipReason(Campaign campaign, CampaignRecipientTarget target)
    {
        if (!target.IsActive)
            return SkipInactiveUser;
        if (!target.MarketingOptIn)
            return SkipNoConsent;
        if (campaign.Channel == CampaignChannel.Email && string.IsNullOrWhiteSpace(target.Email))
            return SkipNoEmail;
        return null;
    }

    private async Task<bool> ClaimAsync(
        int campaignId,
        CampaignRecipientTarget target,
        int attempt,
        BackgroundJobOptions options,
        CancellationToken cancellationToken
    )
    {
        var now = DateTime.UtcNow;
        var leaseUntil = now.AddSeconds(options.CampaignLeaseSeconds);
        var claimed = await db
            .CampaignRecipients.Where(r =>
                r.CampaignId == campaignId
                && r.UserId == target.UserId
                && (
                    r.Status == CampaignRecipientStatus.Pending
                    || r.Status == CampaignRecipientStatus.Failed
                )
                && r.AttemptCount == target.AttemptCount
                && r.AttemptCount < options.CampaignMaxAttempts
                && (r.NextAttemptAt == null || r.NextAttemptAt <= now)
            )
            .ExecuteUpdateAsync(
                setters =>
                    setters
                        .SetProperty(r => r.AttemptCount, attempt)
                        .SetProperty(r => r.NextAttemptAt, leaseUntil),
                cancellationToken
            );
        return claimed > 0;
    }

    private async Task<CampaignEmailPromo> IssueCodeAsync(
        Campaign campaign,
        CampaignRecipientTarget target,
        int attempt,
        CancellationToken cancellationToken
    )
    {
        var promotion = target.PromotionId is { } promotionId
            ? await db
                .Promotions.AsNoTracking()
                .FirstAsync(p => p.PromotionId == promotionId, cancellationToken)
            : await CreateCodeAsync(campaign, target.UserId, attempt, cancellationToken);

        return new CampaignEmailPromo(
            promotion.Code,
            promotion.DiscountType,
            promotion.DiscountValue,
            promotion.MaxDiscountAmount,
            promotion.EndsAt ?? promotion.CreatedAt.AddDays(campaign.PromoValidityDays!.Value)
        );
    }

    private async Task<Promotion> CreateCodeAsync(
        Campaign campaign,
        int userId,
        int attempt,
        CancellationToken cancellationToken
    )
    {
        Promotion? promotion = null;
        try
        {
            await db.InTransactionAsync(
                async () =>
                {
                    var code = await CampaignPromotionFactory.NewCodeAsync(db, cancellationToken);
                    var created = CampaignPromotionFactory.Build(
                        campaign,
                        userId,
                        code,
                        DateTime.UtcNow
                    );
                    promotion = created;
                    db.Promotions.Add(created);
                    await db.SaveChangesAsync(cancellationToken);

                    var linked = await db
                        .CampaignRecipients.Where(r =>
                            r.CampaignId == campaign.CampaignId
                            && r.UserId == userId
                            && r.AttemptCount == attempt
                            && r.PromotionId == null
                        )
                        .ExecuteUpdateAsync(
                            setters => setters.SetProperty(r => r.PromotionId, created.PromotionId),
                            cancellationToken
                        );
                    if (linked == 0)
                        throw new InvalidOperationException("CampaignRecipientCodeLinkLost");
                },
                cancellationToken
            );
            return promotion!;
        }
        finally
        {
            if (promotion is not null)
                db.Promotions.Entry(promotion).State = EntityState.Detached;
        }
    }

    private async Task<bool> TrySkipAsync(int campaignId, int userId, int attempt, string reason) =>
        await db
            .CampaignRecipients.Where(r =>
                r.CampaignId == campaignId
                && r.UserId == userId
                && r.AttemptCount == attempt
                && (
                    r.Status == CampaignRecipientStatus.Pending
                    || r.Status == CampaignRecipientStatus.Failed
                )
            )
            .ExecuteUpdateAsync(
                setters =>
                    setters
                        .SetProperty(r => r.Status, CampaignRecipientStatus.Skipped)
                        .SetProperty(r => r.LastError, reason)
                        .SetProperty(r => r.NextAttemptAt, (DateTime?)null),
                CancellationToken.None
            ) > 0;

    private async Task<bool> TryMarkSentAsync(int campaignId, int userId, int attempt) =>
        await db
            .CampaignRecipients.Where(r =>
                r.CampaignId == campaignId
                && r.UserId == userId
                && r.AttemptCount == attempt
                && (
                    r.Status == CampaignRecipientStatus.Pending
                    || r.Status == CampaignRecipientStatus.Failed
                )
            )
            .ExecuteUpdateAsync(
                setters =>
                    setters
                        .SetProperty(r => r.Status, CampaignRecipientStatus.Sent)
                        .SetProperty(r => r.SentAt, DateTime.UtcNow)
                        .SetProperty(r => r.LastError, (string?)null)
                        .SetProperty(r => r.NextAttemptAt, (DateTime?)null),
                CancellationToken.None
            ) > 0;

    private async Task TryFailAsync(
        int campaignId,
        int userId,
        int attempt,
        Exception exception,
        BackgroundJobOptions options
    )
    {
        var delays = options.CampaignRetryDelaysSeconds;
        var delaySeconds =
            delays.Length == 0 ? 0 : delays[Math.Min(attempt - 1, delays.Length - 1)];
        DateTime? nextAttemptAt =
            attempt < options.CampaignMaxAttempts
                ? DateTime.UtcNow.AddSeconds(delaySeconds)
                : null;
        var error = $"{exception.GetType().Name}: {exception.Message}";
        if (error.Length > FieldLengths.CampaignRecipientError)
            error = error[..FieldLengths.CampaignRecipientError];

        await db
            .CampaignRecipients.Where(r =>
                r.CampaignId == campaignId
                && r.UserId == userId
                && r.AttemptCount == attempt
                && r.Status != CampaignRecipientStatus.Skipped
            )
            .ExecuteUpdateAsync(
                setters =>
                    setters
                        .SetProperty(r => r.Status, CampaignRecipientStatus.Failed)
                        .SetProperty(r => r.SentAt, (DateTime?)null)
                        .SetProperty(r => r.LastError, error)
                        .SetProperty(r => r.NextAttemptAt, nextAttemptAt),
                CancellationToken.None
            );
    }
}
