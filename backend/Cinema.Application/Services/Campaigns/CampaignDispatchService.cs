using Cinema.Application.Interfaces.Audit;
using Cinema.Application.Interfaces.Campaigns;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Options;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cinema.Application.Services.Campaigns;

public class CampaignDispatchService(
    IAppDbContext db,
    ICampaignAudienceService audienceService,
    ICampaignRecipientSender sender,
    IAuditLogger auditLogger,
    IOptions<BackgroundJobOptions> jobOptions,
    ILogger<CampaignDispatchService> logger
) : ICampaignDispatchService
{
    private const int MaxCampaignsPerRun = 20;
    private const string AbandonedReason = "LeaseExpired";

    public async Task<int> DispatchDueAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var dueIds = await db
            .Campaigns.AsNoTracking()
            .Where(c => c.Status == CampaignStatus.Scheduled && c.ScheduledAt <= now)
            .OrderBy(c => c.ScheduledAt)
            .ThenBy(c => c.CampaignId)
            .Take(MaxCampaignsPerRun)
            .Select(c => c.CampaignId)
            .ToListAsync(cancellationToken);
        foreach (var id in dueIds)
        {
            try
            {
                await StartAsync(id, now, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "CampaignStartFailed CampaignId {CampaignId}", id);
            }
        }

        var sendingIds = await db
            .Campaigns.AsNoTracking()
            .Where(c => c.Status == CampaignStatus.Sending)
            .OrderBy(c => c.CampaignId)
            .Take(MaxCampaignsPerRun)
            .Select(c => c.CampaignId)
            .ToListAsync(cancellationToken);

        var sent = 0;
        foreach (var id in sendingIds)
        {
            try
            {
                sent += await ProcessAsync(id, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "CampaignDispatchFailed CampaignId {CampaignId}", id);
            }
        }

        if (sent > 0)
            logger.LogInformation("CampaignMessagesSent Count {Count}", sent);
        return sent;
    }

    private async Task StartAsync(int campaignId, DateTime now, CancellationToken cancellationToken)
    {
        var segmentId = await db
            .Campaigns.AsNoTracking()
            .Where(c => c.CampaignId == campaignId)
            .Select(c => (int?)c.SegmentId)
            .FirstOrDefaultAsync(cancellationToken);
        if (segmentId is null)
            return;

        var segmentActive = await db
            .CustomerSegments.AsNoTracking()
            .AnyAsync(s => s.SegmentId == segmentId && s.IsActive, cancellationToken);

        await db.InTransactionAsync(
            async () =>
            {
                var updated = segmentActive
                    ? await db
                        .Campaigns.Where(c =>
                            c.CampaignId == campaignId
                            && c.Status == CampaignStatus.Scheduled
                            && c.ScheduledAt <= now
                        )
                        .ExecuteUpdateAsync(
                            setters =>
                                setters
                                    .SetProperty(c => c.Status, CampaignStatus.Sending)
                                    .SetProperty(c => c.UpdatedAt, now),
                            cancellationToken
                        )
                    : await db
                        .Campaigns.Where(c =>
                            c.CampaignId == campaignId && c.Status == CampaignStatus.Scheduled
                        )
                        .ExecuteUpdateAsync(
                            setters =>
                                setters
                                    .SetProperty(c => c.Status, CampaignStatus.Draft)
                                    .SetProperty(c => c.ScheduledAt, (DateTime?)null)
                                    .SetProperty(c => c.UpdatedAt, now),
                            cancellationToken
                        );
                if (updated == 0)
                    return;

                auditLogger.Record(
                    segmentActive ? AuditActions.StartCampaign : AuditActions.ReturnCampaignToDraft,
                    AuditEntityTypes.Campaign,
                    campaignId.ToString(),
                    new { Status = CampaignStatus.Scheduled },
                    new
                    {
                        Status = segmentActive ? CampaignStatus.Sending : CampaignStatus.Draft,
                    }
                );
                await db.SaveChangesAsync(cancellationToken);
            },
            cancellationToken
        );
    }

    private async Task<int> ProcessAsync(int campaignId, CancellationToken cancellationToken)
    {
        var campaign = await db
            .Campaigns.AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.CampaignId == campaignId && c.Status == CampaignStatus.Sending,
                cancellationToken
            );
        if (campaign is null)
            return 0;

        var options = jobOptions.Value;
        if (campaign.RecipientsPreparedAt is null)
        {
            await PrepareRecipientsAsync(campaign, options, cancellationToken);
            await db
                .Campaigns.Where(c => c.CampaignId == campaignId && c.RecipientsPreparedAt == null)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(c => c.RecipientsPreparedAt, DateTime.UtcNow),
                    cancellationToken
                );
        }

        await AbandonExhaustedAsync(campaignId, options, cancellationToken);

        var sent = 0;
        var afterUserId = 0;
        while (true)
        {
            var now = DateTime.UtcNow;
            var batch = await db
                .CampaignRecipients.AsNoTracking()
                .Where(r =>
                    r.CampaignId == campaignId
                    && r.UserId > afterUserId
                    && (
                        r.Status == CampaignRecipientStatus.Pending
                        || r.Status == CampaignRecipientStatus.Failed
                    )
                    && r.AttemptCount < options.CampaignMaxAttempts
                    && (r.NextAttemptAt == null || r.NextAttemptAt <= now)
                )
                .OrderBy(r => r.UserId)
                .Take(options.CampaignBatchSize)
                .Select(r => new CampaignRecipientTarget(
                    r.UserId,
                    r.User.FullName,
                    r.User.Email,
                    r.User.IsActive,
                    r.User.CustomerProfile != null && r.User.CustomerProfile.MarketingOptIn,
                    r.PromotionId,
                    r.AttemptCount
                ))
                .ToListAsync(cancellationToken);
            if (batch.Count == 0)
                break;

            afterUserId = batch[^1].UserId;
            foreach (var target in batch)
            {
                var outcome = await sender.SendAsync(campaign, target, cancellationToken);
                if (outcome == CampaignSendOutcome.Sent)
                    sent++;
            }
        }

        await CompleteIfDoneAsync(campaignId, options, cancellationToken);
        return sent;
    }

    private async Task PrepareRecipientsAsync(
        Campaign campaign,
        BackgroundJobOptions options,
        CancellationToken cancellationToken
    )
    {
        var eligible = await audienceService.GetEligibleAsync(campaign, cancellationToken);
        var afterUserId = 0;
        while (true)
        {
            var userIds = await eligible
                .Where(p => p.UserId > afterUserId)
                .OrderBy(p => p.UserId)
                .Take(options.CampaignBatchSize)
                .Select(p => p.UserId)
                .ToArrayAsync(cancellationToken);
            if (userIds.Length == 0)
                break;

            afterUserId = userIds[^1];
            var campaignId = campaign.CampaignId;
            var now = DateTime.UtcNow;
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO "CampaignRecipients" ("CampaignId", "UserId", "Status", "AttemptCount", "CreatedAt")
                SELECT {campaignId}, u, 'Pending', 0, {now}
                FROM unnest({userIds}) AS u
                ON CONFLICT DO NOTHING
                """,
                cancellationToken
            );
        }
    }

    private async Task AbandonExhaustedAsync(
        int campaignId,
        BackgroundJobOptions options,
        CancellationToken cancellationToken
    )
    {
        var now = DateTime.UtcNow;
        await db
            .CampaignRecipients.Where(r =>
                r.CampaignId == campaignId
                && r.Status == CampaignRecipientStatus.Pending
                && r.AttemptCount >= options.CampaignMaxAttempts
                && (r.NextAttemptAt == null || r.NextAttemptAt <= now)
            )
            .ExecuteUpdateAsync(
                setters =>
                    setters
                        .SetProperty(r => r.Status, CampaignRecipientStatus.Failed)
                        .SetProperty(r => r.LastError, AbandonedReason)
                        .SetProperty(r => r.NextAttemptAt, (DateTime?)null),
                cancellationToken
            );
    }

    private async Task CompleteIfDoneAsync(
        int campaignId,
        BackgroundJobOptions options,
        CancellationToken cancellationToken
    )
    {
        var outstanding = await db
            .CampaignRecipients.AsNoTracking()
            .AnyAsync(
                r =>
                    r.CampaignId == campaignId
                    && (
                        r.Status == CampaignRecipientStatus.Pending
                        || (
                            r.Status == CampaignRecipientStatus.Failed
                            && r.AttemptCount < options.CampaignMaxAttempts
                        )
                    ),
                cancellationToken
            );
        if (outstanding)
            return;

        var counts = await db
            .CampaignRecipients.AsNoTracking()
            .Where(r => r.CampaignId == campaignId)
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        int CountOf(CampaignRecipientStatus status) =>
            counts.FirstOrDefault(c => c.Status == status)?.Count ?? 0;

        await db.InTransactionAsync(
            async () =>
            {
                var now = DateTime.UtcNow;
                var updated = await db
                    .Campaigns.Where(c =>
                        c.CampaignId == campaignId && c.Status == CampaignStatus.Sending
                    )
                    .ExecuteUpdateAsync(
                        setters =>
                            setters
                                .SetProperty(c => c.Status, CampaignStatus.Completed)
                                .SetProperty(c => c.UpdatedAt, now),
                        cancellationToken
                    );
                if (updated == 0)
                    return;

                auditLogger.Record(
                    AuditActions.CompleteCampaign,
                    AuditEntityTypes.Campaign,
                    campaignId.ToString(),
                    new { Status = CampaignStatus.Sending },
                    new
                    {
                        Status = CampaignStatus.Completed,
                        Sent = CountOf(CampaignRecipientStatus.Sent),
                        Failed = CountOf(CampaignRecipientStatus.Failed),
                        Skipped = CountOf(CampaignRecipientStatus.Skipped),
                    }
                );
                await db.SaveChangesAsync(cancellationToken);
            },
            cancellationToken
        );
    }
}
