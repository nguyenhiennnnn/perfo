namespace Cinema.Application.DTOs.Campaigns;

public record CampaignPromoRequest(
    string DiscountType,
    decimal DiscountValue,
    decimal? MaxDiscountAmount,
    int ValidityDays
);

public record UpsertCampaignRequest(
    string Name,
    int SegmentId,
    string Channel,
    string Subject,
    string Body,
    CampaignPromoRequest? Promo,
    DateTime? ScheduledAt
);

public record CampaignPromoDto(
    string DiscountType,
    decimal DiscountValue,
    decimal? MaxDiscountAmount,
    int ValidityDays
);

public record CampaignAudienceDto(
    int SegmentCustomers,
    int WithoutConsent,
    int WithoutContact,
    int RecentlyContacted,
    int Eligible
);

public record CampaignDto(
    int CampaignId,
    string Name,
    int SegmentId,
    string SegmentName,
    string Channel,
    string Subject,
    string Body,
    CampaignPromoDto? Promo,
    DateTime? ScheduledAt,
    string Status,
    CampaignAudienceDto? Audience,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CampaignListFilter(string? Search, string? Status, int? SegmentId);

public record DeleteCampaignResponse(int Id, bool Deleted);

public record CampaignRecipientListFilter(string? Search, bool RevealPersonalData);

public record CampaignRecipientDto(int UserId, string FullName, string? Email, bool PersonalDataMasked);

public record CampaignTestSendResponse(string? SentTo);
