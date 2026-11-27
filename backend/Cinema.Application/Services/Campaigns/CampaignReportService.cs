using Cinema.Application.DTOs.Campaigns;
using Cinema.Application.Interfaces.Campaigns;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Campaigns;

public class CampaignReportService(IAppDbContext db) : ICampaignReportService
{
    private const int RatioDecimals = 4;

    public async Task<CampaignReportDto> GetAsync(
        int campaignId,
        CancellationToken cancellationToken
    )
    {
        var campaign =
            await db
                .Campaigns.AsNoTracking()
                .Where(c => c.CampaignId == campaignId)
                .Select(c => new { c.Name, c.Status })
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_Campaign);

        var recipients = db.CampaignRecipients.AsNoTracking().Where(r => r.CampaignId == campaignId);
        var byStatus = await recipients
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);
        var opened = await recipients.CountAsync(
            r => r.Status == CampaignRecipientStatus.Sent && r.FirstOpenedAt != null,
            cancellationToken
        );
        var codesIssued = await recipients.CountAsync(
            r => r.Status == CampaignRecipientStatus.Sent && r.PromotionId != null,
            cancellationToken
        );

        var redemptions = await db
            .PromotionRedemptions.AsNoTracking()
            .Where(x =>
                x.Status == PromotionRedemptionStatus.Redeemed
                && x.Order.Status == OrderStatus.Confirmed
                && db.CampaignRecipients.Any(r =>
                    r.CampaignId == campaignId
                    && r.Status == CampaignRecipientStatus.Sent
                    && r.PromotionId == x.PromotionId
                    && r.UserId == x.UserId
                )
            )
            .Select(x => new
            {
                x.UserId,
                x.OrderId,
                x.DiscountAmount,
                x.Order.FinalAmount,
            })
            .ToListAsync(cancellationToken);

        var orderIds = redemptions.Select(x => x.OrderId).Distinct().ToList();
        var refunds =
            orderIds.Count == 0
                ? []
                : await db
                    .PaymentTransactions.AsNoTracking()
                    .Where(t =>
                        t.Type == PaymentTransactionType.Refund
                        && t.Status == PaymentStatus.Success
                        && orderIds.Contains(t.OrderId)
                    )
                    .GroupBy(t => t.OrderId)
                    .Select(g => new { OrderId = g.Key, Amount = g.Sum(t => t.Amount) })
                    .ToDictionaryAsync(x => x.OrderId, x => x.Amount, cancellationToken);

        var orders = redemptions.GroupBy(x => x.OrderId).Select(g => g.First()).ToList();
        var revenue = orders.Sum(x =>
            ChargeRevenueQuery.NetOf(x.FinalAmount, refunds.GetValueOrDefault(x.OrderId))
        );

        var sent = byStatus.GetValueOrDefault(CampaignRecipientStatus.Sent);
        var codeUsers = redemptions.Select(x => x.UserId).Distinct().Count();

        return new CampaignReportDto(
            campaignId,
            campaign.Name,
            campaign.Status.ToString(),
            byStatus.Values.Sum(),
            sent,
            byStatus.GetValueOrDefault(CampaignRecipientStatus.Failed),
            byStatus.GetValueOrDefault(CampaignRecipientStatus.Skipped),
            byStatus.GetValueOrDefault(CampaignRecipientStatus.Pending),
            opened,
            Ratio(opened, sent),
            codesIssued,
            codeUsers,
            Ratio(codeUsers, sent),
            orders.Count,
            redemptions.Sum(x => x.DiscountAmount),
            revenue
        );
    }

    private static decimal Ratio(int part, int total) =>
        total > 0 ? Math.Round((decimal)part / total, RatioDecimals) : 0m;
}
