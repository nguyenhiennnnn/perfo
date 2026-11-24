using Cinema.Application.Events;
using Cinema.Application.Interfaces;
using Cinema.Application.Services.Audit;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Invoices;

public class InvoiceRefundAdjustmentHandler(
    IAppDbContext db,
    IAuditLogger auditLogger,
    INotificationService notificationService
) : IBackgroundEventHandler<OrderRefunded>
{
    public async Task HandleAsync(OrderRefunded businessEvent, CancellationToken cancellationToken)
    {
        if (businessEvent.Amount <= 0m)
            return;

        var request = await db
            .InvoiceRequests.Include(r => r.Order)
            .FirstOrDefaultAsync(
                r =>
                    r.OrderId == businessEvent.OrderId && r.Status == InvoiceRequestStatus.Issued,
                cancellationToken
            );
        if (request is null)
            return;

        if (
            request.AdjustmentRefundTransactionId is { } lastId
            && lastId >= businessEvent.RefundTransactionId
        )
            return;

        var before = AuditSnapshots.InvoiceRequestState(request);
        request.AdjustmentRefundAmount = request.NeedsAdjustment
            ? request.AdjustmentRefundAmount + businessEvent.Amount
            : businessEvent.Amount;
        request.AdjustmentRefundTransactionId = businessEvent.RefundTransactionId;
        request.NeedsAdjustment = true;
        request.AdjustmentFlaggedAt = businessEvent.OccurredAt;
        request.AdjustmentResolvedAt = null;
        request.AdjustmentResolvedByUserId = null;
        request.AdjustmentNote = null;
        auditLogger.Record(
            AuditActions.FlagInvoiceAdjustment,
            AuditEntityTypes.InvoiceRequest,
            request.InvoiceRequestId.ToString(),
            before,
            AuditSnapshots.InvoiceRequestState(request)
        );
        await db.SaveChangesAsync(cancellationToken);

        var recipients = await db
            .Users.AsNoTracking()
            .Where(u =>
                u.IsActive && u.Role.RoleName == Roles.Admin && u.Email != null && u.Email != ""
            )
            .Select(u => u.Email)
            .Distinct()
            .ToListAsync(cancellationToken);
        foreach (var email in recipients)
            notificationService.SendInvoiceAdjustmentNeeded(
                email,
                request.Order.OrderCode,
                request.InvoiceNumber ?? string.Empty,
                request.AdjustmentRefundAmount
            );
    }
}
