using Cinema.Application.DTOs.Campaigns;
using Cinema.Application.DTOs.Common;
using Cinema.Application.Interfaces.Audit;
using Cinema.Application.Interfaces.Campaigns;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Segments;
using Cinema.Application.Services.Segments;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Campaigns;

public class CampaignAudienceService(
    IAppDbContext db,
    ICustomerSegmentQueryService segmentQuery,
    IAuditLogger auditLogger
) : ICampaignAudienceService
{
    private sealed record Audience(
        SegmentConditionNode Conditions,
        IQueryable<CustomerSegmentProfile> Reachable,
        IQueryable<CustomerSegmentProfile> Eligible
    );

    public async Task<IQueryable<CustomerSegmentProfile>> GetEligibleAsync(
        Campaign campaign,
        CancellationToken cancellationToken
    ) => (await BuildAsync(campaign, cancellationToken)).Eligible;

    public async Task<CampaignAudienceDto> SummarizeAsync(
        Campaign campaign,
        CancellationToken cancellationToken
    )
    {
        var audience = await BuildAsync(campaign, cancellationToken);
        var (customers, consented) = await segmentQuery.CountAsync(
            audience.Conditions,
            cancellationToken
        );
        var reachable = await audience.Reachable.CountAsync(cancellationToken);
        var eligible = await audience.Eligible.CountAsync(cancellationToken);

        return new CampaignAudienceDto(
            customers,
            customers - consented,
            consented - reachable,
            reachable - eligible,
            eligible
        );
    }

    public async Task<OffsetPageResult<CampaignRecipientDto>> GetRecipientsAsync(
        int campaignId,
        int page,
        int pageSize,
        CampaignRecipientListFilter filter,
        CancellationToken cancellationToken
    )
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, Pagination.MaxPageSize);

        var campaign =
            await db
                .Campaigns.AsNoTracking()
                .FirstOrDefaultAsync(c => c.CampaignId == campaignId, cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_Campaign);

        var query = (await BuildAsync(campaign, cancellationToken)).Eligible.ApplySearch(
            filter.Search,
            (q, pattern) => q.Where(p => EF.Functions.ILike(p.User.FullName, pattern))
        );

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderBy(p => p.UserId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.UserId,
                p.User.FullName,
                p.User.Email,
            })
            .ToListAsync(cancellationToken);

        if (filter.RevealPersonalData)
        {
            auditLogger.Record(
                AuditActions.RevealCampaignRecipients,
                AuditEntityTypes.Campaign,
                campaignId.ToString(),
                null,
                new
                {
                    page,
                    pageSize,
                    userIds = rows.Select(r => r.UserId),
                }
            );
            await db.SaveChangesAsync(cancellationToken);
        }

        var mask = !filter.RevealPersonalData;
        return OffsetPageResult<CampaignRecipientDto>.Create(
            rows.Select(r => new CampaignRecipientDto(
                    r.UserId,
                    r.FullName,
                    mask ? PersonalDataMasker.MaskEmail(r.Email) : r.Email,
                    mask
                ))
                .ToList(),
            page,
            pageSize,
            total
        );
    }

    private async Task<Audience> BuildAsync(Campaign campaign, CancellationToken cancellationToken)
    {
        var json =
            await db
                .CustomerSegments.AsNoTracking()
                .Where(s => s.SegmentId == campaign.SegmentId)
                .Select(s => s.ConditionJson)
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_CustomerSegment);
        var conditions = SegmentConditionCompiler.Deserialize(json);

        var settings =
            await db
                .CinemaSettings.AsNoTracking()
                .FirstOrDefaultAsync(
                    s => s.Id == AppDbContextExtensions.CinemaSettingsId,
                    cancellationToken
                ) ?? new CinemaSettings();
        var cutoff = DateTime.UtcNow.AddDays(-settings.CampaignMinIntervalDays);
        var campaignId = campaign.CampaignId;

        var consented = segmentQuery.MarketingMembers(conditions);
        var reachable =
            campaign.Channel == CampaignChannel.Email
                ? consented.Where(p => p.User.Email.Trim() != "")
                : consented;
        var contacted = db
            .CampaignRecipients.AsNoTracking()
            .Where(r =>
                r.Status != CampaignRecipientStatus.Failed
                && r.Status != CampaignRecipientStatus.Skipped
                && (r.CampaignId == campaignId || r.CreatedAt >= cutoff)
            );
        var eligible = reachable.Where(p => !contacted.Any(r => r.UserId == p.UserId));

        return new Audience(conditions, reachable, eligible);
    }
}
