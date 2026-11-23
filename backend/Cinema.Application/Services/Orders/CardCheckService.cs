using Cinema.Application.DTOs.Orders;
using Cinema.Application.Interfaces;
using Cinema.Application.Services.Payments;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.Orders;

public class CardCheckService(
    IAppDbContext db,
    IAuditLogger auditLogger,
    ILogger<CardCheckService> logger
) : ICardCheckService
{
    public async Task<RecordCardNotPresentedResponse> RecordNotPresentedAsync(
        int employeeUserId,
        RecordCardNotPresentedRequest request,
        CancellationToken cancellationToken
    )
    {
        var orderId =
            await db
                .OrderLines.AsNoTracking()
                .Where(l => l.OrderLineId == request.OrderLineId)
                .Select(l => (int?)l.OrderId)
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_OrderLine);

        var now = DateTime.UtcNow;

        await db.InTransactionAsync(
            async () =>
            {
                await db.LockOrderAsync(orderId, cancellationToken);

                var affected = await db
                    .OrderLines.Where(l =>
                        l.OrderLineId == request.OrderLineId
                        && l.LineType == OrderLineType.Ticket
                        && l.RequiresCardPresentation
                        && l.CardCheckStatus == CardCheckStatus.Pending
                        && l.RefundedQuantity < l.Quantity
                        && db.Orders.Any(o =>
                            o.OrderId == l.OrderId && o.Status == OrderStatus.Confirmed
                        )
                    )
                    .ExecuteUpdateAsync(
                        setters =>
                            setters
                                .SetProperty(l => l.CardCheckStatus, CardCheckStatus.NotPresented)
                                .SetProperty(l => l.CardCheckRecordedAt, now)
                                .SetProperty(l => l.CardCheckRecordedByUserId, employeeUserId),
                        cancellationToken
                    );

                if (affected == 0)
                    await ThrowForUnchangedLineAsync(request.OrderLineId, cancellationToken);

                auditLogger.Record(
                    AuditActions.CardNotPresented,
                    AuditEntityTypes.OrderLine,
                    request.OrderLineId.ToString(),
                    new { orderId, cardCheckStatus = nameof(CardCheckStatus.Pending) },
                    new
                    {
                        orderId,
                        cardCheckStatus = nameof(CardCheckStatus.NotPresented),
                        recordedAt = now,
                    }
                );
                await db.SaveChangesAsync(cancellationToken);
            },
            cancellationToken
        );

        logger.LogInformation(
            "CardNotPresented OrderId {OrderId} OrderLineId {OrderLineId}",
            orderId,
            request.OrderLineId
        );
        return new RecordCardNotPresentedResponse(
            request.OrderLineId,
            orderId,
            CardCheckStatus.NotPresented.ToString(),
            now
        );
    }

    private async Task ThrowForUnchangedLineAsync(
        int orderLineId,
        CancellationToken cancellationToken
    )
    {
        var line =
            await db
                .OrderLines.AsNoTracking()
                .Where(l => l.OrderLineId == orderLineId)
                .Select(l => new
                {
                    l.LineType,
                    l.RequiresCardPresentation,
                    l.CardCheckStatus,
                })
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_OrderLine);

        if (line.LineType != OrderLineType.Ticket || !line.RequiresCardPresentation)
            throw new BusinessRuleException(Messages.Rule_CardCheckNotRequired);
        if (line.CardCheckStatus is CardCheckStatus.NotPresented or CardCheckStatus.Supplemented)
            throw new ConflictAppException(Messages.Conflict_CardCheckAlreadyRecorded);

        throw new BusinessRuleException(Messages.Rule_CardCheckOrderInvalid);
    }
}
