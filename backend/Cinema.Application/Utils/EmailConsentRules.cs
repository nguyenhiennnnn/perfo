using Cinema.Domain.Entities;
using Cinema.Domain.Enums;

namespace Cinema.Application.Utils;

public static class EmailConsentRules
{
    public static bool CanSend(CustomerProfile? profile, EmailCategory category) =>
        category switch
        {
            EmailCategory.Service => true,
            _ when profile is null => false,
            EmailCategory.Marketing => profile.MarketingOptIn,
            EmailCategory.MovieRecommendation => profile.MarketingOptIn
                && profile.PersonalizedRecommendations,
            EmailCategory.ReminderAndReviewInvite => profile.ReminderAndReviewInvites,
            _ => false,
        };
}
