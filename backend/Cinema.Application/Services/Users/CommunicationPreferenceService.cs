using Cinema.Application.DTOs.Auth;
using Cinema.Application.DTOs.Users;
using Cinema.Application.Interfaces;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Users;

public class CommunicationPreferenceService(
    IAppDbContext db,
    IUnsubscribeTokenService unsubscribeTokenService,
    IReferralCodeService referralCodeService
) : ICommunicationPreferenceService
{
    public async Task<CommunicationPreferencesDto> GetAsync(
        int userId,
        CancellationToken cancellationToken
    )
    {
        var profile = await GetOrCreateProfileAsync(userId, cancellationToken);
        return ToDto(profile);
    }

    public async Task<CommunicationPreferencesDto> UpdateAsync(
        int userId,
        UpdateCommunicationPreferencesRequest request,
        CancellationToken cancellationToken
    )
    {
        var profile = await GetOrCreateProfileAsync(userId, cancellationToken);
        profile.MarketingOptIn = request.MarketingOptIn;
        profile.PersonalizedRecommendations = request.PersonalizedRecommendations;
        profile.ReminderAndReviewInvites = request.ReminderAndReviewInvites;
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(profile);
    }

    public async Task<MessageResponse> UnsubscribeMarketingAsync(
        string token,
        CancellationToken cancellationToken
    )
    {
        var userId =
            unsubscribeTokenService.TryReadUserId(token)
            ?? throw new ValidationAppException(Messages.Invalid_UnsubscribeToken);

        await db
            .CustomerProfiles.Where(p => p.UserId == userId && p.MarketingOptIn)
            .ExecuteUpdateAsync(
                s => s.SetProperty(p => p.MarketingOptIn, false),
                cancellationToken
            );

        return new MessageResponse(Messages.Success_MarketingUnsubscribed);
    }

    public async Task<bool> CanSendAsync(
        int userId,
        EmailCategory category,
        CancellationToken cancellationToken
    )
    {
        if (category == EmailCategory.Service)
            return true;

        var profile = await db
            .CustomerProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        return EmailConsentRules.CanSend(profile, category);
    }

    private async Task<CustomerProfile> GetOrCreateProfileAsync(
        int userId,
        CancellationToken cancellationToken
    )
    {
        var profile = await db.CustomerProfiles.FirstOrDefaultAsync(
            p => p.UserId == userId,
            cancellationToken
        );

        if (profile is null)
        {
            if (!await db.Users.AnyAsync(u => u.UserId == userId, cancellationToken))
                throw new NotFoundException(Messages.NotFound_User);

            profile = new CustomerProfile
            {
                UserId = userId,
                ReferralCode = await referralCodeService.GenerateUniqueAsync(cancellationToken),
            };
            db.CustomerProfiles.Add(profile);
            await db.SaveChangesAsync(cancellationToken);
            return profile;
        }

        if (profile.ReferralCode is null)
        {
            profile.ReferralCode = await referralCodeService.GenerateUniqueAsync(
                cancellationToken
            );
            await db.SaveChangesAsync(cancellationToken);
        }

        return profile;
    }

    private static CommunicationPreferencesDto ToDto(CustomerProfile profile) =>
        new(
            profile.MarketingOptIn,
            profile.PersonalizedRecommendations,
            profile.ReminderAndReviewInvites,
            profile.ReferralCode
        );
}
