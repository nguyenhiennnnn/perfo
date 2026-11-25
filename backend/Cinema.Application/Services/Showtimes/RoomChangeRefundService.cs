using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Showtimes;
using Cinema.Application.Interfaces.Audit;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Orders;
using Cinema.Application.Interfaces.Payments;
using Cinema.Application.Services.Audit;
using Cinema.Application.Services.Payments;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.Showtimes;

public class RoomChangeRefundService(
    IAppDbContext db,
    IOrderRefundService orderRefunds,
    IPaymentRefundService paymentRefunds,
    IAuditLogger auditLogger,
    ILogger<RoomChangeRefundService> logger
) : IRoomChangeRefundService
{
    private const string StatusAll = "All";

    public async Task<OffsetPageResult<RoomChangeRefundListItemDto>> GetRefundDuesAsync(
        RoomChangeRefundListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        var query = db.RoomChangeRefundDues.AsNoTracking().AsQueryable();

        if (
            !string.IsNullOrWhiteSpace(filter.Status)
            && !string.Equals(filter.Status, StatusAll, StringComparison.OrdinalIgnoreCase)
            && Enum.TryParse<RoomChangeRefundStatus>(filter.Status.Trim(), true, out var status)
        )
            query = query.Where(d => d.Status == status);

        if (filter.ShowtimeId is { } showtimeId)
            query = query.Where(d => d.RoomChange.ShowtimeId == showtimeId);

        if (filter.OrderId is { } orderId)
            query = query.Where(d => d.OrderId == orderId);

        if (!string.IsNullOrWhiteSpace(filter.OrderCode))
        {
            var pattern = SearchHelper.ILikePattern(filter.OrderCode.Trim());
            query = query.Where(d => EF.Functions.ILike(d.Order.OrderCode, pattern));
        }

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderBy(d => d.Status == RoomChangeRefundStatus.Pending ? 0 : 1)
            .ThenByDescending(d => d.CreatedAt)
            .ThenByDescending(d => d.RoomChangeRefundDueId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new RoomChangeRefundListItemDto(
                d.RoomChangeRefundDueId,
                d.ShowtimeRoomChangeId,
                d.RoomChange.ShowtimeId,
                d.RoomChange.Showtime.Movie.Title,
                d.RoomChange.Showtime.StartTime,
                d.OrderId,
                d.Order.OrderCode,
                d.Order.UserId,
                d.Order.CustomerName ?? (d.Order.User != null ? d.Order.User.FullName : null) ?? string.Empty,
                d.Order.CustomerPhone,
                d.Order.UserId == null,
                d.OrderLineId,
                d.FromSeat.SeatRow + d.FromSeat.SeatNumber,
                d.ToSeat.SeatRow + d.ToSeat.SeatNumber,
                d.AmountDue,
                d.Status.ToString(),
                d.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return OffsetPageResult<RoomChangeRefundListItemDto>.Create(rows, page, pageSize, total);
    }

    public async Task<RoomChangeRefundConfirmResultDto> ConfirmAsync(
        int refundDueId,
        int confirmedByUserId,
        CancellationToken cancellationToken
    )
    {
        var orderId =
            await db
                .RoomChangeRefundDues.AsNoTracking()
                .Where(d => d.RoomChangeRefundDueId == refundDueId)
                .Select(d => (int?)d.OrderId)
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_RoomChangeRefundDue);

        RoomChangeRefundConfirmResultDto? result = null;

        await db.InTransactionAsync(
            async () =>
            {
                await db.LockOrderAsync(orderId, cancellationToken);

                var due = await db
                    .RoomChangeRefundDues.AsNoTracking()
                    .FirstAsync(d => d.RoomChangeRefundDueId == refundDueId, cancellationToken);
                var order = await db
                    .Orders.AsNoTracking()
                    .FirstAsync(o => o.OrderId == orderId, cancellationToken);

                if (due.Status == RoomChangeRefundStatus.Refunded)
                {
                    result = Result(due, order.OrderCode, true, []);
                    return;
                }

                if (due.Status == RoomChangeRefundStatus.Voided)
                    throw new BusinessRuleException(Messages.Rule_RoomChangeRefundVoided);

                if (order.Status != OrderStatus.Confirmed)
                    throw new BusinessRuleException(Messages.Rule_RoomChangeRefundOrderNotConfirmed);

                var claimed = await db
                    .RoomChangeRefundDues.Where(d =>
                        d.RoomChangeRefundDueId == refundDueId
                        && d.Status == RoomChangeRefundStatus.Pending
                    )
                    .ExecuteUpdateAsync(
                        s => s.SetProperty(d => d.Status, RoomChangeRefundStatus.Refunded),
                        cancellationToken
                    );
                if (claimed == 0)
                    throw new ConflictAppException(Messages.Conflict_RoomChangeRefundConcurrent);

                var amount = due.AmountDue;
                var lineUpdated = await db
                    .OrderLines.Where(l =>
                        l.OrderLineId == due.OrderLineId
                        && l.OrderId == due.OrderId
                        && l.RefundedAmount + amount <= l.PaidAmount
                    )
                    .ExecuteUpdateAsync(
                        s => s.SetProperty(l => l.RefundedAmount, l => l.RefundedAmount + amount),
                        cancellationToken
                    );
                if (lineUpdated == 0)
                    throw new BusinessRuleException(Messages.Refund_ExceedsCollected);

                var plan = await orderRefunds.BuildPlanForAmountAsync(orderId, amount, cancellationToken);
                var note = string.Format(Messages.Refund_RoomChangeDueDescription, order.OrderCode);
                var transactions = await RecordRefundsAsync(
                    plan,
                    confirmedByUserId,
                    note,
                    cancellationToken
                );

                due.Status = RoomChangeRefundStatus.Refunded;
                auditLogger.Record(
                    AuditActions.ManualRefund,
                    AuditEntityTypes.RoomChangeRefundDue,
                    refundDueId.ToString(),
                    AuditSnapshots.RoomChangeRefundBeforeConfirm(due),
                    AuditSnapshots.RoomChangeRefundAfterConfirm(due, confirmedByUserId, transactions)
                );
                await db.SaveChangesAsync(cancellationToken);

                result = Result(due, order.OrderCode, false, transactions);
            },
            cancellationToken
        );

        logger.LogInformation(
            "RoomChangeRefundConfirmed RefundDueId {RefundDueId} OrderId {OrderId} Amount {Amount} AlreadyRefunded {AlreadyRefunded} ConfirmedByUserId {UserId}",
            refundDueId,
            orderId,
            result!.Amount,
            result.AlreadyRefunded,
            confirmedByUserId
        );
        return result;
    }

    private async Task<IReadOnlyList<RoomChangeRefundTransactionDto>> RecordRefundsAsync(
        OrderRefundPlan plan,
        int confirmedByUserId,
        string note,
        CancellationToken cancellationToken
    )
    {
        var transactions = new List<RoomChangeRefundTransactionDto>();

        var giftCardItems = plan.Items.Where(i => i.Method == PaymentMethod.GiftCard).ToList();
        if (giftCardItems.Count > 0)
        {
            var reservations = await orderRefunds.ReserveAsync(
                new OrderRefundPlan(plan.OrderId, giftCardItems.Sum(i => i.Amount), 0m, giftCardItems),
                confirmedByUserId,
                note,
                cancellationToken
            );
            transactions.AddRange(
                reservations.Select(r => new RoomChangeRefundTransactionDto(
                    r.RefundTransactionId,
                    r.Method.ToDtoMethod(),
                    r.Amount,
                    r.Status.ToDtoStatus()
                ))
            );
        }

        var otherItems = plan.Items.Where(i => i.Method != PaymentMethod.GiftCard).ToList();
        if (otherItems.Count == 0)
            return transactions;

        var chargeIds = otherItems.Select(i => i.ChargeTransactionId).ToList();
        var charges = await db
            .PaymentTransactions.AsNoTracking()
            .Where(t => chargeIds.Contains(t.PaymentTransactionId) && t.OrderId == plan.OrderId)
            .ToDictionaryAsync(t => t.PaymentTransactionId, cancellationToken);

        foreach (var item in otherItems)
        {
            if (!charges.TryGetValue(item.ChargeTransactionId, out var charge))
                throw new NotFoundException(Messages.NotFound_Payment);

            var refund = await paymentRefunds.RecordManualAsync(
                charge,
                item.Amount,
                note,
                confirmedByUserId,
                cancellationToken
            );
            transactions.Add(
                new RoomChangeRefundTransactionDto(
                    refund.PaymentTransactionId,
                    refund.Method.ToDtoMethod(),
                    refund.Amount,
                    refund.Status.ToDtoStatus()
                )
            );
        }

        return transactions;
    }

    private static RoomChangeRefundConfirmResultDto Result(
        RoomChangeRefundDue due,
        string orderCode,
        bool alreadyRefunded,
        IReadOnlyList<RoomChangeRefundTransactionDto> transactions
    ) =>
        new(
            due.RoomChangeRefundDueId,
            due.OrderId,
            orderCode,
            due.AmountDue,
            RoomChangeRefundStatus.Refunded.ToString(),
            alreadyRefunded,
            transactions
        );
}
