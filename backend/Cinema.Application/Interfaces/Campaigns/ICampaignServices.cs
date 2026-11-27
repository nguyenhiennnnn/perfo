using Cinema.Application.DTOs.Campaigns;
using Cinema.Application.DTOs.Common;
using Cinema.Domain.Entities;

namespace Cinema.Application.Interfaces.Campaigns;

public interface ICampaignAdminService
{
    Task<OffsetPageResult<CampaignDto>> GetAsync(
        int page,
        int pageSize,
        CampaignListFilter filter,
        CancellationToken cancellationToken
    );

    Task<CampaignDto> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<CampaignDto> CreateAsync(
        UpsertCampaignRequest request,
        CancellationToken cancellationToken
    );

    Task<CampaignDto> UpdateAsync(
        int id,
        UpsertCampaignRequest request,
        CancellationToken cancellationToken
    );

    Task<DeleteCampaignResponse> DeleteAsync(int id, CancellationToken cancellationToken);
}

public interface ICampaignAudienceService
{
    Task<IQueryable<CustomerSegmentProfile>> GetEligibleAsync(
        Campaign campaign,
        CancellationToken cancellationToken
    );

    Task<CampaignAudienceDto> SummarizeAsync(
        Campaign campaign,
        CancellationToken cancellationToken
    );

    Task<OffsetPageResult<CampaignRecipientDto>> GetRecipientsAsync(
        int campaignId,
        int page,
        int pageSize,
        CampaignRecipientListFilter filter,
        CancellationToken cancellationToken
    );
}

public interface ICampaignDispatchService
{
    Task<int> DispatchDueAsync(CancellationToken cancellationToken);
}

public sealed record CampaignRecipientTarget(
    int UserId,
    string FullName,
    string? Email,
    bool IsActive,
    bool MarketingOptIn,
    int? PromotionId,
    int AttemptCount
);

public enum CampaignSendOutcome
{
    Busy,
    Sent,
    Skipped,
    Failed,
}

public interface ICampaignRecipientSender
{
    Task<CampaignSendOutcome> SendAsync(
        Campaign campaign,
        CampaignRecipientTarget target,
        CancellationToken cancellationToken
    );
}

public interface ICampaignTestSendService
{
    Task<CampaignTestSendResponse> SendAsync(
        int campaignId,
        int adminUserId,
        CancellationToken cancellationToken
    );
}
