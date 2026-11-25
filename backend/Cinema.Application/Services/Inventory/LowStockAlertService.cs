using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Inventory;
using Cinema.Application.Interfaces.Notifications;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.Inventory;

public class LowStockAlertService(
    IAppDbContext db,
    INotificationService notifications,
    ILogger<LowStockAlertService> logger
) : ILowStockAlertService
{
    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        await db
            .InventoryItems.Where(i => i.LowStockAlertedAt != null)
            .Where(InventoryStockRules.IsAboveThresholdExpression)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(i => i.LowStockAlertedAt, (DateTime?)null),
                cancellationToken
            );

        var candidateIds = await db
            .InventoryItems.AsNoTracking()
            .Where(i => i.IsActive && i.LowStockAlertedAt == null)
            .Where(InventoryStockRules.IsLowStockExpression)
            .OrderBy(i => i.InventoryItemId)
            .Select(i => i.InventoryItemId)
            .ToListAsync(cancellationToken);
        if (candidateIds.Count == 0)
            return 0;

        var claimedAt = DateTime.UtcNow;
        var claimedIds = new List<int>();
        foreach (var itemId in candidateIds)
        {
            var claimed = await db
                .InventoryItems.Where(i =>
                    i.InventoryItemId == itemId && i.IsActive && i.LowStockAlertedAt == null
                )
                .Where(InventoryStockRules.IsLowStockExpression)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(i => i.LowStockAlertedAt, (DateTime?)claimedAt),
                    cancellationToken
                );
            if (claimed == 1)
                claimedIds.Add(itemId);
        }

        if (claimedIds.Count == 0)
            return 0;

        var delivered = false;
        try
        {
            delivered = await SendAsync(claimedIds, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "LowStockAlertSendFailed Items {Count}", claimedIds.Count);
        }
        catch (OperationCanceledException)
        {
            await ReleaseClaimAsync(claimedIds, claimedAt);
            throw;
        }

        if (!delivered)
        {
            await ReleaseClaimAsync(claimedIds, claimedAt);
            return 0;
        }

        logger.LogInformation("LowStockAlertSent Items {Count}", claimedIds.Count);
        return claimedIds.Count;
    }

    private async Task<bool> SendAsync(List<int> itemIds, CancellationToken cancellationToken)
    {
        var lines = await db
            .InventoryItems.AsNoTracking()
            .Where(i => itemIds.Contains(i.InventoryItemId))
            .OrderBy(i => i.Name)
            .ThenBy(i => i.InventoryItemId)
            .Select(i => new LowStockEmailLine(
                i.Code,
                i.Name,
                i.Unit,
                i.QuantityOnHand,
                i.LowStockThreshold
            ))
            .ToListAsync(cancellationToken);
        if (lines.Count == 0)
            return false;

        var recipients = await db
            .Users.AsNoTracking()
            .Where(u =>
                u.IsActive && u.Role.RoleName == Roles.Admin && u.Email != null && u.Email != ""
            )
            .Select(u => u.Email)
            .Distinct()
            .ToListAsync(cancellationToken);
        if (recipients.Count == 0)
        {
            logger.LogWarning("LowStockAlertNoRecipients Items {Count}", lines.Count);
            return false;
        }

        var sent = 0;
        foreach (var email in recipients)
        {
            try
            {
                notifications.SendLowStockAlert(email, lines);
                sent++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "LowStockAlertEnqueueFailed");
            }
        }

        return sent > 0;
    }

    private async Task ReleaseClaimAsync(List<int> itemIds, DateTime claimedAt)
    {
        await db
            .InventoryItems.Where(i =>
                itemIds.Contains(i.InventoryItemId) && i.LowStockAlertedAt == claimedAt
            )
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(i => i.LowStockAlertedAt, (DateTime?)null),
                CancellationToken.None
            );
    }
}
