using Cinema.Application.DTOs.Campaigns;
using Cinema.Application.Interfaces.Audit;
using Cinema.Application.Interfaces.Auth;
using Cinema.Application.Interfaces.Campaigns;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Notifications;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Campaigns;

public class CampaignTestSendService(
    IAppDbContext db,
    INotificationService notificationService,
    IUnsubscribeTokenService unsubscribeTokens,
    IAuditLogger auditLogger
) : ICampaignTestSendService
{
    public async Task<CampaignTestSendResponse> SendAsync(
        int campaignId,
        int adminUserId,
        CancellationToken cancellationToken
    )
    {
        var campaign =
            await db
                .Campaigns.AsNoTracking()
                .FirstOrDefaultAsync(c => c.CampaignId == campaignId, cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_Campaign);
        var admin =
            await db
                .Users.AsNoTracking()
                .Where(u => u.UserId == adminUserId)
                .Select(u => new { u.FullName, u.Email })
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_User);
        if (string.IsNullOrWhiteSpace(admin.Email))
            throw new BusinessRuleException(Messages.Rule_Campaign_TestNoEmail);

        var promo = campaign.PromoDiscountType is { } type
            ? new CampaignEmailPromo(
                Messages.Campaign_TestSampleCode,
                type,
                campaign.PromoDiscountValue!.Value,
                campaign.PromoMaxDiscountAmount,
                DateTime.UtcNow.AddDays(campaign.PromoValidityDays!.Value)
            )
            : null;
        var unsubscribeLink =
            campaign.Channel == CampaignChannel.Email
                ? unsubscribeTokens.CreateUnsubscribeUrl(adminUserId)
                : null;

        notificationService.SendCampaign(
            admin.Email,
            new CampaignEmailData(
                admin.FullName,
                campaign.Subject,
                campaign.Body,
                promo,
                unsubscribeLink,
                true
            )
        );

        auditLogger.Record(
            AuditActions.SendCampaignTest,
            AuditEntityTypes.Campaign,
            campaignId.ToString(),
            null,
            new { adminUserId }
        );
        await db.SaveChangesAsync(cancellationToken);

        return new CampaignTestSendResponse(PersonalDataMasker.MaskEmail(admin.Email));
    }
}
