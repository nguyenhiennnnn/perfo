using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Infrastructure.Persistence.Seed.Orders;

internal sealed record SeedBuyer(int UserId, string FullName, string? Phone);

internal sealed record SeedStaff(int EmployeeId, int UserId);

internal sealed record SeedTicket(OrderLine Line, ShowtimeSeat Seat);

internal static class OrderSeedKit
{
    public const string VnPaySuccessCode = "00";
    public static readonly string ComboHoldType = nameof(Combo);

    public static Order NewOrder(
        string code,
        OrderType type,
        SalesChannel channel,
        DateTime createdAt,
        int holdMinutes,
        int? userId,
        string customerName,
        string? customerPhone,
        int? employeeId
    ) =>
        new()
        {
            OrderCode = code,
            Type = type,
            Channel = channel,
            UserId = userId,
            CustomerName = customerName,
            CustomerPhone = customerPhone,
            Status = OrderStatus.Pending,
            CreatedAt = createdAt,
            ExpiresAt = createdAt.AddMinutes(holdMinutes),
            CreatedByEmployeeId = employeeId,
        };

    public static List<SeedTicket> AddTickets(Order order, SeedSeatAllocation allocation)
    {
        var tickets = new List<SeedTicket>(allocation.Seats.Count);
        foreach (var seat in allocation.Seats)
        {
            var line = new OrderLine
            {
                LineType = OrderLineType.Ticket,
                ShowtimeId = allocation.Showtime.ShowtimeId,
                ShowtimeSeatId = seat.ShowtimeSeatId,
                Quantity = 1,
                UnitListPrice = seat.Price,
                LockedUnitPrice = seat.Price,
                PaidAmount = seat.Price,
            };
            order.Lines.Add(line);
            tickets.Add(new SeedTicket(line, seat));
        }

        return tickets;
    }

    public static OrderLine AddCombo(Order order, Combo combo, int quantity, int? showtimeId)
    {
        var line = new OrderLine
        {
            LineType = OrderLineType.Combo,
            ShowtimeId = showtimeId,
            ComboId = combo.ComboId,
            Quantity = quantity,
            UnitListPrice = combo.Price,
            LockedUnitPrice = combo.Price,
            PaidAmount = combo.Price * quantity,
        };
        order.Lines.Add(line);
        return line;
    }

    public static async Task<MembershipTier?> ResolveTierAsync(
        AppDbContext db,
        int? userId,
        DateTime now,
        CancellationToken cancellationToken
    )
    {
        if (userId is null)
            return null;

        var hasProfile = await db.CustomerProfiles.AnyAsync(
            p => p.UserId == userId,
            cancellationToken
        );
        if (!hasProfile)
            return null;

        var points = await LoyaltyWindow.SumActiveAsync(db, userId.Value, now, cancellationToken);
        return await LoyaltyWindow.ResolveTierAsync(db, points, cancellationToken);
    }

    public static void ApplyTierDiscount(Order order, MembershipTier? tier)
    {
        if (tier is null)
            return;

        var tickets = order.Lines.Where(l => l.LineType == OrderLineType.Ticket).ToList();
        var ticketAmount = tickets.Sum(l => l.LockedUnitPrice);
        var amount = Math.Min(
            OrderPricing.RoundToDong(ticketAmount * tier.DiscountPercent / 100m),
            ticketAmount
        );
        if (amount <= 0m)
            return;

        var adjustment = new OrderPriceAdjustment
        {
            Type = PriceAdjustmentType.TierDiscount,
            Amount = amount,
            ReferenceType = nameof(MembershipTier),
            ReferenceId = tier.TierId,
        };
        var shares = OrderPricing.AllocateProportionally(
            amount,
            tickets.Select(l => l.LockedUnitPrice).ToList()
        );
        for (var i = 0; i < tickets.Count; i++)
        {
            if (shares[i] <= 0m)
                continue;

            adjustment.Allocations.Add(
                new OrderPriceAdjustmentAllocation { OrderLine = tickets[i], Amount = shares[i] }
            );
            tickets[i].PaidAmount -= shares[i];
        }

        order.PriceAdjustments.Add(adjustment);
    }

    public static void FinalizeAmounts(Order order)
    {
        order.TotalAmount = order.Lines.Sum(l => l.LockedUnitPrice * l.Quantity);
        order.DiscountAmount = order.PriceAdjustments.Sum(a => a.Amount);
        order.FinalAmount = order.TotalAmount - order.DiscountAmount;
    }

    public static PaymentTransaction AddGatewayCharge(
        Order order,
        PaymentMethod method,
        decimal amount,
        DateTime at,
        int index = 0
    )
    {
        var charge = new PaymentTransaction
        {
            Type = PaymentTransactionType.Charge,
            Method = method,
            Amount = amount,
            TransactionCode = TransactionCode(order.OrderCode, at, index),
            GatewayTransactionId = OrderSeedCodes.GatewayTransactionId(order.OrderCode, index),
            GatewayResponseCode = VnPaySuccessCode,
            Status = PaymentStatus.Success,
            CreatedAt = at,
            CompletedAt = at.AddMinutes(1),
        };
        order.PaymentTransactions.Add(charge);
        return charge;
    }

    public static PaymentTransaction AddFailedGatewayCharge(
        Order order,
        PaymentMethod method,
        decimal amount,
        DateTime at,
        int index = 0
    )
    {
        var charge = new PaymentTransaction
        {
            Type = PaymentTransactionType.Charge,
            Method = method,
            Amount = amount,
            TransactionCode = TransactionCode(order.OrderCode, at, index),
            Status = PaymentStatus.Failed,
            CreatedAt = at,
        };
        order.PaymentTransactions.Add(charge);
        return charge;
    }

    public static PaymentTransaction AddCashCharge(
        Order order,
        decimal amount,
        decimal received,
        DateTime at,
        int performedByUserId,
        int index = 0
    )
    {
        var charge = new PaymentTransaction
        {
            Type = PaymentTransactionType.Charge,
            Method = PaymentMethod.Cash,
            Amount = amount,
            TransactionCode = TransactionCode(order.OrderCode, at, index),
            Status = PaymentStatus.Success,
            CreatedAt = at,
            CompletedAt = at,
            PerformedByUserId = performedByUserId,
            AmountReceived = received,
            ChangeGiven = received - amount,
        };
        order.PaymentTransactions.Add(charge);
        return charge;
    }

    public static PaymentTransaction AddCardCharge(
        Order order,
        decimal amount,
        DateTime at,
        int performedByUserId,
        int index = 0
    )
    {
        var charge = new PaymentTransaction
        {
            Type = PaymentTransactionType.Charge,
            Method = PaymentMethod.Card,
            Amount = amount,
            TransactionCode = TransactionCode(order.OrderCode, at, index),
            Status = PaymentStatus.Success,
            CreatedAt = at,
            CompletedAt = at,
            PerformedByUserId = performedByUserId,
        };
        order.PaymentTransactions.Add(charge);
        return charge;
    }

    public static PaymentTransaction AddRefund(
        Order order,
        PaymentTransaction charge,
        decimal amount,
        DateTime at,
        int? performedByUserId,
        string? note,
        int index
    )
    {
        var refund = new PaymentTransaction
        {
            Type = PaymentTransactionType.Refund,
            Method = charge.Method,
            Amount = amount,
            TransactionCode = $"{charge.TransactionCode}R{index:D4}",
            GatewayTransactionId = charge.GatewayTransactionId is null
                ? null
                : $"RF{charge.GatewayTransactionId}{index}",
            Status = PaymentStatus.Success,
            CreatedAt = at,
            CompletedAt = at,
            PerformedByUserId = performedByUserId,
            OriginalTransaction = charge,
            Note = note,
        };
        order.PaymentTransactions.Add(refund);
        return refund;
    }

    public static void IssueTickets(
        Order order,
        IReadOnlyList<SeedTicket> tickets,
        bool keepSeatsBooked
    )
    {
        foreach (var ticket in tickets)
        {
            ticket.Line.QrCode = CodeGenerator.GenerateTicketQrPayload(
                order.OrderCode,
                ticket.Seat.Seat.SeatRow,
                ticket.Seat.Seat.SeatNumber
            );

            if (!keepSeatsBooked)
                continue;

            ticket.Seat.Status = ShowtimeSeatStatus.Booked;
            ticket.Seat.HeldByUserId = null;
            ticket.Seat.HeldByOrderId = null;
            ticket.Seat.HeldUntil = null;
        }
    }

    public static void CheckIn(IReadOnlyList<SeedTicket> tickets, DateTime at)
    {
        foreach (var ticket in tickets)
        {
            ticket.Line.IsCheckedIn = true;
            ticket.Line.CheckedInAt = at;
        }
    }

    public static void FulfillCombos(Order order)
    {
        foreach (var line in order.Lines.Where(l => l.LineType == OrderLineType.Combo))
            line.QuantityFulfilled = line.Quantity;
    }

    public static void AddComboHolds(Order order, ResourceHoldStatus status, DateTime resolvedAt)
    {
        foreach (var line in order.Lines.Where(l => l.LineType == OrderLineType.Combo))
            order.ResourceHolds.Add(
                new OrderResourceHold
                {
                    ResourceType = ComboHoldType,
                    ResourceId = line.ComboId,
                    Quantity = line.Quantity,
                    Status = status,
                    CreatedAt = order.CreatedAt,
                    ResolvedAt = status == ResourceHoldStatus.Reserved ? null : resolvedAt,
                }
            );
    }

    public static void MarkCancelled(
        Order order,
        CancelReason reason,
        CancelledBy cancelledBy,
        DateTime at
    )
    {
        order.Status = OrderStatus.Cancelled;
        order.CancelReason = reason;
        order.CancelledBy = cancelledBy;
        order.CancelledAt = at;
    }

    public static decimal RoundUpTo(decimal value, decimal step) =>
        Math.Ceiling(value / step) * step;

    public static async Task ApplyLoyaltyAsync(
        AppDbContext db,
        Order order,
        DateTime confirmedAt,
        DateTime now,
        decimal pointsPerAmount,
        IReadOnlyList<decimal> refundedAmounts,
        CancellationToken cancellationToken
    )
    {
        if (order.UserId is not { } userId)
            return;

        var profile = await db.CustomerProfiles.FirstOrDefaultAsync(
            p => p.UserId == userId,
            cancellationToken
        );
        if (profile is null)
            return;

        var paid = order
            .Lines.Where(l => l.LineType is OrderLineType.Ticket or OrderLineType.Combo)
            .Sum(l => l.PaidAmount);
        var basePoints = await LoyaltyWindow.SumActiveAsync(db, userId, now, cancellationToken);
        var earned = LoyaltyWindow.PointsFor(paid, pointsPerAmount);

        var revokedPoints = 0;
        var revokedSpend = 0m;
        foreach (var refunded in refundedAmounts)
        {
            var reversalBase = OrderRefundCalculator.LoyaltyReversalBase(
                paid,
                order.FinalAmount,
                refunded
            );
            revokedSpend += reversalBase;
            revokedPoints = Math.Min(
                earned,
                revokedPoints + LoyaltyWindow.PointsFor(reversalBase, pointsPerAmount)
            );
        }

        if (earned > 0)
            db.LoyaltyPointEntries.Add(
                new LoyaltyPointEntry
                {
                    UserId = userId,
                    Order = order,
                    Points = earned,
                    RevokedPoints = revokedPoints,
                    EarnedAt = confirmedAt,
                }
            );

        profile.TotalSpent = Math.Max(0m, profile.TotalSpent + paid - revokedSpend);
        profile.Points = basePoints + earned - revokedPoints;
        profile.TierId = (
            await LoyaltyWindow.ResolveTierAsync(db, profile.Points, cancellationToken)
        )?.TierId;
    }

    private static string TransactionCode(string orderCode, DateTime at, int index)
    {
        var utc = DateTime.SpecifyKind(at, DateTimeKind.Utc);
        var unix = new DateTimeOffset(utc).ToUnixTimeSeconds();
        return $"{orderCode}{unix}{index}";
    }
}
