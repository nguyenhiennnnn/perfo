using Cinema.Application.Interfaces.Campaigns;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Campaigns;

public class CampaignOpenTracker(IAppDbContext db, ICampaignOpenTokenService tokens)
    : ICampaignOpenTracker
{
    public async Task RecordAsync(string token, CancellationToken cancellationToken)
    {
        if (tokens.TryRead(token) is not { } ids)
            return;

        var now = DateTime.UtcNow;
        await db
            .CampaignRecipients.Where(r =>
                r.CampaignId == ids.CampaignId
                && r.UserId == ids.UserId
                && r.Status == CampaignRecipientStatus.Sent
                && r.FirstOpenedAt == null
                && r.User.CustomerProfile != null
                && r.User.CustomerProfile.MarketingOptIn
            )
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(r => r.FirstOpenedAt, (DateTime?)now),
                cancellationToken
            );
    }
}
