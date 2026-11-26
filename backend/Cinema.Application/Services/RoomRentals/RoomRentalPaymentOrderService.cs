using Cinema.Application.DTOs.RoomRentals;
using Cinema.Application.Interfaces.Audit;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Ledger;
using Cinema.Application.Interfaces.RoomRentals;
using Cinema.Application.Options;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cinema.Application.Services.RoomRentals;

public class RoomRentalPaymentOrderService(
    IAppDbContext db,
    IOptions<BookingOptions> bookingOptions,
    IPaymentLedgerService ledger,
    IAuditLogger auditLogger,
    ILogger<RoomRentalPaymentOrderService> logger
) : IRoomRentalPaymentOrderService
{
    private static readonly RoomRentalPaymentStage[] Stages =
    [
        RoomRentalPaymentStage.Deposit,
        RoomRentalPaymentStage.Balance,
    ];

    public async Task<RoomRentalPaymentsDto> GetAsync(
        int requestId,
        CancellationToken cancellationToken
    )
    {
        var request =
            await db
                .RoomRentalRequests.AsNoTracking()
                .Where(r => r.RoomRentalRequestId == requestId)
                .Select(r => new { r.RequestCode, r.Status })
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_RoomRentalRequest);

        var quote = await RoomRentalPaymentPlan.LoadLatestSentQuoteAsync(
            db,
            requestId,
            cancellationToken
        );

        var stages = new List<RoomRentalPaymentStageDto>();
        foreach (var stage in Stages)
        {
            var order = await FindActiveAsync(requestId, stage, cancellationToken);
            var dto = order is null ? null : await ToDtoAsync(order, cancellationToken);
            var amount = Math.Max(0m, RoomRentalPaymentPlan.AmountFor(quote, stage));
            stages.Add(
                new RoomRentalPaymentStageDto(
                    stage,
                    amount,
                    RoomRentalPaymentPlan.DueAt(quote, stage),
                    dto,
                    order?.Status == OrderStatus.Confirmed
                )
            );
        }

        return new RoomRentalPaymentsDto(
            requestId,
            request.RequestCode,
            request.Status,
            quote.Total,
            stages.Where(s => s.Order is not null).Sum(s => s.Order!.PaidAmount),
            stages
        );
    }

    public async Task<RoomRentalPaymentOrderDto> EnsureOrderAsync(
        int requestId,
        RoomRentalPaymentStage stage,
        CancellationToken cancellationToken
    )
    {
        var request =
            await db
                .RoomRentalRequests.AsNoTracking()
                .FirstOrDefaultAsync(r => r.RoomRentalRequestId == requestId, cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_RoomRentalRequest);

        var existing = await FindActiveAsync(requestId, stage, cancellationToken);
        if (existing is not null)
            return await ToDtoAsync(existing, cancellationToken);

        if (request.Status != RoomRentalPaymentPlan.RequiredStatus(stage))
            throw new BusinessRuleException(
                stage == RoomRentalPaymentStage.Deposit
                    ? Messages.Rule_RoomRentalDepositStatusInvalid
                    : Messages.Rule_RoomRentalBalanceStatusInvalid
            );

        var quote = await RoomRentalPaymentPlan.LoadLatestSentQuoteAsync(
            db,
            requestId,
            cancellationToken
        );
        var amount = RoomRentalPaymentPlan.AmountFor(quote, stage);
        if (amount <= 0m)
            throw new BusinessRuleException(
                stage == RoomRentalPaymentStage.Balance
                    ? Messages.Rule_RoomRentalNoBalanceDue
                    : Messages.Rule_RoomRentalDepositStatusInvalid
            );

        var now = DateTime.UtcNow;
        var expiresAt = RoomRentalPaymentPlan.ExpiresAt(quote, stage);
        if (expiresAt <= now)
            throw new BusinessRuleException(Messages.Rule_RoomRentalPaymentWindowClosed);

        var order = new Order
        {
            OrderCode = await OrderCodes.GenerateUniqueAsync(
                db,
                bookingOptions.Value.BookingCodeMaxAttempts,
                cancellationToken
            ),
            Type = OrderType.RoomRental,
            Channel = SalesChannel.Online,
            CustomerName = CustomerName(request),
            CustomerPhone = request.ContactPhone,
            Status = OrderStatus.Pending,
            CreatedAt = now,
            ExpiresAt = expiresAt,
            RoomRentalRequestId = requestId,
            RoomRentalPaymentStage = stage,
        };
        order.Lines.Add(
            new OrderLine
            {
                LineType = OrderLineType.Fee,
                Description = string.Format(
                    stage == RoomRentalPaymentStage.Deposit
                        ? Messages.RoomRental_DepositLineDescription
                        : Messages.RoomRental_BalanceLineDescription,
                    request.RequestCode
                ),
                Quantity = 1,
                UnitListPrice = amount,
                LockedUnitPrice = amount,
            }
        );
        OrderPricing.ApplyTotals(order);

        db.Orders.Add(order);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            db.Entry(order).State = EntityState.Detached;
            var raced = await FindActiveAsync(requestId, stage, cancellationToken);
            if (raced is null)
                throw;
            return await ToDtoAsync(raced, cancellationToken);
        }

        auditLogger.Record(
            AuditActions.CreateRoomRentalOrder,
            AuditEntityTypes.Order,
            order.OrderId.ToString(),
            null,
            new
            {
                requestId,
                stage,
                order.OrderCode,
                order.FinalAmount,
                order.ExpiresAt,
            }
        );
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "RoomRentalOrderCreated RequestId {RequestId} OrderId {OrderId} Stage {Stage} Amount {Amount}",
            requestId,
            order.OrderId,
            stage,
            amount
        );
        return await ToDtoAsync(order, cancellationToken);
    }

    private Task<Order?> FindActiveAsync(
        int requestId,
        RoomRentalPaymentStage stage,
        CancellationToken cancellationToken
    ) =>
        db
            .Orders.AsNoTracking()
            .Where(o =>
                o.RoomRentalRequestId == requestId
                && o.RoomRentalPaymentStage == stage
                && (o.Status == OrderStatus.Pending || o.Status == OrderStatus.Confirmed)
            )
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<RoomRentalPaymentOrderDto> ToDtoAsync(
        Order order,
        CancellationToken cancellationToken
    )
    {
        var summary = await ledger.GetSummaryAsync(order.OrderId, cancellationToken);
        return new RoomRentalPaymentOrderDto(
            order.OrderId,
            order.OrderCode,
            order.RoomRentalPaymentStage!.Value,
            order.Status.ToDtoStatus(),
            order.FinalAmount,
            summary.NetPaidAmount,
            order.Status == OrderStatus.Confirmed ? 0m : summary.OutstandingAmount,
            order.ExpiresAt
        );
    }

    private static string CustomerName(RoomRentalRequest request)
    {
        var name = string.IsNullOrWhiteSpace(request.OrganizationName)
            ? request.FullName
            : request.OrganizationName;
        return name.Length <= FieldLengths.FullName ? name : name[..FieldLengths.FullName];
    }
}
