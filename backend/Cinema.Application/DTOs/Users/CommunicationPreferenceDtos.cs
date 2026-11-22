namespace Cinema.Application.DTOs.Users;

public record CommunicationPreferencesDto(
    bool MarketingOptIn,
    bool PersonalizedRecommendations,
    bool ReminderAndReviewInvites,
    string? ReferralCode
);

public record UpdateCommunicationPreferencesRequest(
    bool MarketingOptIn,
    bool PersonalizedRecommendations,
    bool ReminderAndReviewInvites
);

public record UnsubscribeRequest(string Token);
