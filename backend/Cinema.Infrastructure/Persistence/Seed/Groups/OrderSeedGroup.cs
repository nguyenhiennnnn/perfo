using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Infrastructure.Persistence.Seed.Orders;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Infrastructure.Persistence.Seed.Groups;

internal sealed class OrderSeedGroup : ISeedGroup
{
    private sealed record Context(
        AppDbContext Db,
        ILogger Logger,
        DateTime Now,
        CinemaSettings Settings,
        OrderSeedPool Pool,
        IReadOnlyDictionary<string, Combo> Combos,
        IReadOnlyDictionary<string, SeedBuyer> Buyers,
        IReadOnlyDictionary<string, SeedStaff> Staff,
        int? AdminUserId,
        CancellationToken CancellationToken
    );

    private sealed record OnlineSpec(
        string Code,
        string CustomerEmail,
        SeedWhen When,
        string? ScreenType,
        int Seats,
        string? ComboName,
        int ComboQuantity,
        Func<SeedShowtime, DateTime, DateTime> CreatedAt
    );

    private sealed record CounterSpec(
        string Code,
        SeedWhen When,
        string? ScreenType,
        int Seats,
        string? ComboName,
        int ComboQuantity,
        string StaffEmail,
        string CustomerName,
        string CustomerPhone,
        TimeSpan Age
    );

    private sealed record Draft(
        Order Order,
        SeedSeatAllocation Allocation,
        IReadOnlyList<SeedTicket> Tickets,
        SeedBuyer? Buyer,
        SeedStaff? Staff
    );

    public string Name => "orders";

    public async Task SeedAsync(SeedContext context, CancellationToken cancellationToken)
    {
        if (!context.SeedDemoScenarios)
            return;

        var db = context.Db;
        var now = DateTime.UtcNow;
        var settings =
            await db.CinemaSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken)
            ?? new CinemaSettings();
        var pool = await OrderSeedPool.LoadAsync(
            db,
            now,
            settings.CancelDeadlineHours,
            cancellationToken
        );

        var c = new Context(
            db,
            context.Logger,
            now,
            settings,
            pool,
            await LoadCombosAsync(db, cancellationToken),
            await LoadBuyersAsync(db, cancellationToken),
            await LoadStaffAsync(db, cancellationToken),
            await db
                .Users.AsNoTracking()
                .Where(u => u.Email == SeedEmails.Admin)
                .Select(u => (int?)u.UserId)
                .FirstOrDefaultAsync(cancellationToken),
            cancellationToken
        );

        await SeedConfirmedOnlineAsync(
            c,
            new OnlineSpec(
                OrderSeedCodes.OnlineConfirmedTier,
                SeedEmails.Customer3,
                SeedWhen.Tomorrow,
                RoomCatalogSeedGroup.ScreenType3D,
                3,
                OrderSeedCombos.Couple,
                1,
                (_, reference) => reference.AddHours(-6)
            )
        );
        await SeedConfirmedOnlineAsync(
            c,
            new OnlineSpec(
                OrderSeedCodes.OnlineConfirmedImax,
                SeedEmails.Customer4,
                SeedWhen.Tomorrow,
                RoomCatalogSeedGroup.ScreenTypeImax,
                2,
                OrderSeedCombos.Premium,
                1,
                (_, reference) => reference.AddHours(-3)
            )
        );
        await SeedExpiredOnlineAsync(
            c,
            new OnlineSpec(
                OrderSeedCodes.OnlineExpired,
                SeedEmails.Customer1,
                SeedWhen.Upcoming,
                RoomCatalogSeedGroup.ScreenType2D,
                2,
                OrderSeedCombos.Solo,
                1,
                (_, reference) => reference.AddHours(-3)
            )
        );
        await SeedCancelledByCustomerAsync(
            c,
            new OnlineSpec(
                OrderSeedCodes.OnlineCancelled,
                SeedEmails.Customer2,
                SeedWhen.Tomorrow,
                RoomCatalogSeedGroup.ScreenType2D,
                2,
                null,
                0,
                (_, reference) => reference.AddHours(-20)
            )
        );
        await SeedCancelledShowtimeAsync(
            c,
            new OnlineSpec(
                OrderSeedCodes.ShowtimeCancelled,
                SeedEmails.CustomerNew,
                SeedWhen.Cancelled,
                null,
                2,
                OrderSeedCombos.Solo,
                1,
                (_, reference) => reference.AddDays(-2)
            )
        );
        await SeedPartialRefundAsync(
            c,
            new OnlineSpec(
                OrderSeedCodes.PartialRefund,
                SeedEmails.Customer3,
                SeedWhen.Upcoming,
                null,
                2,
                OrderSeedCombos.Solo,
                3,
                (_, reference) => reference.AddHours(-30)
            )
        );
        await SeedCheckedInAsync(
            c,
            new OnlineSpec(
                OrderSeedCodes.CheckedIn,
                SeedEmails.Customer1,
                SeedWhen.Past,
                null,
                2,
                OrderSeedCombos.Couple,
                1,
                (showtime, _) => showtime.StartTime.AddHours(-26)
            )
        );
        await SeedCounterCashAsync(
            c,
            new CounterSpec(
                OrderSeedCodes.CounterCash,
                SeedWhen.Soon,
                RoomCatalogSeedGroup.ScreenType2D,
                2,
                OrderSeedCombos.Couple,
                1,
                SeedEmails.Employee,
                "Trần Văn Hùng",
                "0987654321",
                TimeSpan.FromMinutes(40)
            )
        );
        await SeedCounterMixedAsync(
            c,
            new CounterSpec(
                OrderSeedCodes.CounterMixed,
                SeedWhen.Soon,
                null,
                4,
                OrderSeedCombos.Family,
                1,
                SeedEmails.ShiftLead,
                "Nguyễn Thị Mai",
                "0971234567",
                TimeSpan.FromHours(1)
            )
        );
        await SeedCounterComboOnlyAsync(c);
        await SeedOnlineComboOnlyAsync(c);
    }

    private static async Task SeedConfirmedOnlineAsync(Context c, OnlineSpec spec)
    {
        if (await ExistsAsync(c, spec.Code))
            return;

        var draft = await DraftOnlineAsync(c, spec);
        if (draft is null)
            return;

        var order = draft.Order;
        var paidAt = order.CreatedAt.AddMinutes(3);
        OrderSeedKit.AddGatewayCharge(order, PaymentMethod.VNPay, order.FinalAmount, paidAt);
        OrderSeedKit.IssueTickets(order, draft.Tickets, keepSeatsBooked: true);
        OrderSeedKit.AddComboHolds(order, ResourceHoldStatus.Committed, paidAt);
        order.Status = OrderStatus.Confirmed;

        await SaveAsync(c, order, paidAt, []);
    }

    private static async Task SeedExpiredOnlineAsync(Context c, OnlineSpec spec)
    {
        if (await ExistsAsync(c, spec.Code))
            return;

        var draft = await DraftOnlineAsync(c, spec);
        if (draft is null)
            return;

        var order = draft.Order;
        OrderSeedKit.AddFailedGatewayCharge(
            order,
            PaymentMethod.VNPay,
            order.FinalAmount,
            order.CreatedAt.AddMinutes(1)
        );
        OrderSeedKit.AddComboHolds(order, ResourceHoldStatus.Released, order.ExpiresAt!.Value);
        order.Status = OrderStatus.Expired;

        await SaveAsync(c, order, null, []);
    }

    private static async Task SeedCancelledByCustomerAsync(Context c, OnlineSpec spec)
    {
        if (await ExistsAsync(c, spec.Code))
            return;

        var draft = await DraftOnlineAsync(c, spec);
        if (draft is null)
            return;

        await CompleteCancelledAsync(
            c,
            draft,
            CancelReason.CustomerChangedMind,
            CancelledBy.Customer,
            c.Now.AddHours(-1),
            c.Settings.CancelRefundPercent,
            draft.Buyer!.UserId,
            string.Format(Messages.Refund_CancelOrderDescription, spec.Code)
        );
    }

    private static async Task SeedCancelledShowtimeAsync(Context c, OnlineSpec spec)
    {
        if (await ExistsAsync(c, spec.Code))
            return;

        var draft = await DraftOnlineAsync(c, spec);
        if (draft is null)
            return;

        await CompleteCancelledAsync(
            c,
            draft,
            CancelReason.ShowtimeCancelled,
            CancelledBy.Admin,
            c.Now.AddDays(-1),
            100m,
            c.AdminUserId,
            string.Format(Messages.Refund_ShowtimeCancelledDescription, spec.Code)
        );
    }

    private static async Task CompleteCancelledAsync(
        Context c,
        Draft draft,
        CancelReason reason,
        CancelledBy cancelledBy,
        DateTime cancelledAt,
        decimal refundPercent,
        int? performedByUserId,
        string note
    )
    {
        var order = draft.Order;
        var paidAt = order.CreatedAt.AddMinutes(3);
        var charge = OrderSeedKit.AddGatewayCharge(
            order,
            PaymentMethod.VNPay,
            order.FinalAmount,
            paidAt
        );
        OrderSeedKit.IssueTickets(order, draft.Tickets, keepSeatsBooked: false);
        OrderSeedKit.AddComboHolds(order, ResourceHoldStatus.Committed, paidAt);

        var refunds = new List<decimal>();
        var refundAmount = Math.Min(
            order.FinalAmount,
            OrderPricing.RoundToDong(order.FinalAmount * refundPercent / 100m)
        );
        if (refundAmount > 0m)
        {
            OrderSeedKit.AddRefund(
                order,
                charge,
                refundAmount,
                cancelledAt,
                performedByUserId,
                note,
                1
            );
            refunds.Add(refundAmount);
        }

        OrderSeedKit.MarkCancelled(order, reason, cancelledBy, cancelledAt);

        await SaveAsync(c, order, paidAt, refunds);
    }

    private static async Task SeedPartialRefundAsync(Context c, OnlineSpec spec)
    {
        if (await ExistsAsync(c, spec.Code))
            return;

        var draft = await DraftOnlineAsync(c, spec);
        if (draft is null)
            return;

        var order = draft.Order;
        var paidAt = order.CreatedAt.AddMinutes(3);
        var charge = OrderSeedKit.AddGatewayCharge(
            order,
            PaymentMethod.VNPay,
            order.FinalAmount,
            paidAt
        );
        OrderSeedKit.IssueTickets(order, draft.Tickets, keepSeatsBooked: true);
        OrderSeedKit.AddComboHolds(order, ResourceHoldStatus.Committed, paidAt);
        order.Status = OrderStatus.Confirmed;

        var comboLine = order.Lines.First(l => l.LineType == OrderLineType.Combo);
        var refunds = new List<decimal>();
        for (var step = 1; step <= 2; step++)
        {
            var amount = OrderRefundCalculator.LineRefundAmount(
                comboLine.PaidAmount,
                comboLine.Quantity,
                comboLine.RefundedQuantity,
                1,
                100m
            );
            comboLine.RefundedAmount += amount;
            comboLine.RefundedQuantity += 1;
            OrderSeedKit.AddRefund(
                order,
                charge,
                amount,
                c.Now.AddHours(-8 + (step - 1) * 4),
                c.AdminUserId,
                null,
                step
            );
            refunds.Add(amount);
        }

        await SaveAsync(c, order, paidAt, refunds);
    }

    private static async Task SeedCheckedInAsync(Context c, OnlineSpec spec)
    {
        if (await ExistsAsync(c, spec.Code))
            return;

        var draft = await DraftOnlineAsync(c, spec);
        if (draft is null)
            return;

        var order = draft.Order;
        var paidAt = order.CreatedAt.AddMinutes(3);
        OrderSeedKit.AddGatewayCharge(order, PaymentMethod.VNPay, order.FinalAmount, paidAt);
        OrderSeedKit.IssueTickets(order, draft.Tickets, keepSeatsBooked: true);
        OrderSeedKit.CheckIn(draft.Tickets, draft.Allocation.Showtime.StartTime.AddMinutes(-10));
        OrderSeedKit.FulfillCombos(order);
        OrderSeedKit.AddComboHolds(order, ResourceHoldStatus.Committed, paidAt);
        order.Status = OrderStatus.Confirmed;

        await SaveAsync(c, order, paidAt, []);
    }

    private static async Task SeedCounterCashAsync(Context c, CounterSpec spec)
    {
        if (await ExistsAsync(c, spec.Code))
            return;

        var draft = await DraftCounterAsync(c, spec);
        if (draft is null)
            return;

        var order = draft.Order;
        var staff = draft.Staff!;
        var paidAt = order.CreatedAt.AddMinutes(2);
        OrderSeedKit.AddCashCharge(
            order,
            order.FinalAmount,
            OrderSeedKit.RoundUpTo(order.FinalAmount, 50_000m),
            paidAt,
            staff.UserId
        );
        OrderSeedKit.IssueTickets(order, draft.Tickets, keepSeatsBooked: true);
        OrderSeedKit.AddComboHolds(order, ResourceHoldStatus.Committed, paidAt);
        order.Status = OrderStatus.Confirmed;

        await SaveAsync(c, order, paidAt, []);
    }

    private static async Task SeedCounterMixedAsync(Context c, CounterSpec spec)
    {
        if (await ExistsAsync(c, spec.Code))
            return;

        var draft = await DraftCounterAsync(c, spec);
        if (draft is null)
            return;

        var order = draft.Order;
        var staff = draft.Staff!;
        var paidAt = order.CreatedAt.AddMinutes(2);
        var cashAmount = Math.Floor(order.FinalAmount * 0.4m / 10_000m) * 10_000m;
        var cardAmount = order.FinalAmount - cashAmount;
        OrderSeedKit.AddCashCharge(
            order,
            cashAmount,
            OrderSeedKit.RoundUpTo(cashAmount, 50_000m),
            paidAt,
            staff.UserId
        );
        OrderSeedKit.AddCardCharge(order, cardAmount, paidAt.AddMinutes(1), staff.UserId, index: 1);
        OrderSeedKit.IssueTickets(order, draft.Tickets, keepSeatsBooked: true);
        OrderSeedKit.AddComboHolds(order, ResourceHoldStatus.Committed, paidAt);
        order.Status = OrderStatus.Confirmed;

        await SaveAsync(c, order, paidAt, []);
    }

    private static async Task SeedCounterComboOnlyAsync(Context c)
    {
        const string code = OrderSeedCodes.CounterComboOnly;
        if (await ExistsAsync(c, code))
            return;

        if (
            !c.Staff.TryGetValue(SeedEmails.Employee, out var staff)
            || !c.Combos.TryGetValue(OrderSeedCombos.Couple, out var couple)
            || !c.Combos.TryGetValue(OrderSeedCombos.Caramel, out var caramel)
        )
        {
            c.Logger.LogWarning("Skip seeding order {OrderCode}: missing staff or combos.", code);
            return;
        }

        var order = OrderSeedKit.NewOrder(
            code,
            OrderType.Combo,
            SalesChannel.Counter,
            c.Now.AddHours(-5),
            c.Settings.HoldDurationMinutes,
            null,
            "Lê Hoàng Nam",
            "0965432109",
            staff.EmployeeId
        );
        OrderSeedKit.AddCombo(order, couple, 2, null);
        OrderSeedKit.AddCombo(order, caramel, 1, null);
        OrderSeedKit.FinalizeAmounts(order);

        var paidAt = order.CreatedAt.AddMinutes(1);
        OrderSeedKit.AddCashCharge(
            order,
            order.FinalAmount,
            OrderSeedKit.RoundUpTo(order.FinalAmount, 50_000m),
            paidAt,
            staff.UserId
        );
        OrderSeedKit.FulfillCombos(order);
        OrderSeedKit.AddComboHolds(order, ResourceHoldStatus.Committed, paidAt);
        order.Status = OrderStatus.Confirmed;

        await SaveAsync(c, order, paidAt, []);
    }

    private static async Task SeedOnlineComboOnlyAsync(Context c)
    {
        const string code = OrderSeedCodes.OnlineComboOnly;
        if (await ExistsAsync(c, code))
            return;

        if (
            !c.Buyers.TryGetValue(SeedEmails.Customer2, out var buyer)
            || !c.Combos.TryGetValue(OrderSeedCombos.Premium, out var premium)
            || !c.Combos.TryGetValue(OrderSeedCombos.Solo, out var solo)
        )
        {
            c.Logger.LogWarning(
                "Skip seeding order {OrderCode}: missing customer or combos.",
                code
            );
            return;
        }

        var order = OrderSeedKit.NewOrder(
            code,
            OrderType.Combo,
            SalesChannel.Online,
            c.Now.AddHours(-2),
            c.Settings.HoldDurationMinutes,
            buyer.UserId,
            buyer.FullName,
            buyer.Phone,
            null
        );
        OrderSeedKit.AddCombo(order, premium, 1, null);
        OrderSeedKit.AddCombo(order, solo, 1, null);
        OrderSeedKit.FinalizeAmounts(order);

        var paidAt = order.CreatedAt.AddMinutes(3);
        OrderSeedKit.AddGatewayCharge(order, PaymentMethod.VNPay, order.FinalAmount, paidAt);
        OrderSeedKit.AddComboHolds(order, ResourceHoldStatus.Committed, paidAt);
        order.Status = OrderStatus.Confirmed;

        await SaveAsync(c, order, paidAt, []);
    }

    private static async Task<Draft?> DraftOnlineAsync(Context c, OnlineSpec spec)
    {
        if (!c.Buyers.TryGetValue(spec.CustomerEmail, out var buyer))
        {
            c.Logger.LogWarning(
                "Skip seeding order {OrderCode}: customer {Email} not found.",
                spec.Code,
                spec.CustomerEmail
            );
            return null;
        }

        Combo? combo = null;
        if (spec.ComboName is not null && !c.Combos.TryGetValue(spec.ComboName, out combo))
        {
            c.Logger.LogWarning(
                "Skip seeding order {OrderCode}: combo {Combo} not found.",
                spec.Code,
                spec.ComboName
            );
            return null;
        }

        var allocation = await c.Pool.AllocateAsync(
            c.Db,
            spec.When,
            spec.ScreenType,
            spec.Seats,
            c.CancellationToken
        );
        if (allocation is null)
        {
            c.Logger.LogWarning(
                "Skip seeding order {OrderCode}: no suitable showtime with free seats.",
                spec.Code
            );
            return null;
        }

        var order = OrderSeedKit.NewOrder(
            spec.Code,
            OrderType.Ticket,
            SalesChannel.Online,
            spec.CreatedAt(allocation.Showtime, c.Now),
            c.Settings.HoldDurationMinutes,
            buyer.UserId,
            buyer.FullName,
            buyer.Phone,
            null
        );
        var tickets = OrderSeedKit.AddTickets(order, allocation);
        if (combo is not null)
            OrderSeedKit.AddCombo(order, combo, spec.ComboQuantity, allocation.Showtime.ShowtimeId);

        OrderSeedKit.ApplyTierDiscount(
            order,
            await OrderSeedKit.ResolveTierAsync(c.Db, buyer.UserId, c.Now, c.CancellationToken)
        );
        OrderSeedKit.FinalizeAmounts(order);

        return new Draft(order, allocation, tickets, buyer, null);
    }

    private static async Task<Draft?> DraftCounterAsync(Context c, CounterSpec spec)
    {
        if (!c.Staff.TryGetValue(spec.StaffEmail, out var staff))
        {
            c.Logger.LogWarning(
                "Skip seeding order {OrderCode}: staff {Email} not found.",
                spec.Code,
                spec.StaffEmail
            );
            return null;
        }

        Combo? combo = null;
        if (spec.ComboName is not null && !c.Combos.TryGetValue(spec.ComboName, out combo))
        {
            c.Logger.LogWarning(
                "Skip seeding order {OrderCode}: combo {Combo} not found.",
                spec.Code,
                spec.ComboName
            );
            return null;
        }

        var allocation = await c.Pool.AllocateAsync(
            c.Db,
            spec.When,
            spec.ScreenType,
            spec.Seats,
            c.CancellationToken
        );
        if (allocation is null)
        {
            c.Logger.LogWarning(
                "Skip seeding order {OrderCode}: no suitable showtime with free seats.",
                spec.Code
            );
            return null;
        }

        var order = OrderSeedKit.NewOrder(
            spec.Code,
            OrderType.Ticket,
            SalesChannel.Counter,
            c.Now - spec.Age,
            c.Settings.HoldDurationMinutes,
            null,
            spec.CustomerName,
            spec.CustomerPhone,
            staff.EmployeeId
        );
        var tickets = OrderSeedKit.AddTickets(order, allocation);
        if (combo is not null)
            OrderSeedKit.AddCombo(order, combo, spec.ComboQuantity, allocation.Showtime.ShowtimeId);

        OrderSeedKit.FinalizeAmounts(order);

        return new Draft(order, allocation, tickets, null, staff);
    }

    private static async Task SaveAsync(
        Context c,
        Order order,
        DateTime? confirmedAt,
        IReadOnlyList<decimal> refundedAmounts
    )
    {
        if (confirmedAt is { } at)
            await OrderSeedKit.ApplyLoyaltyAsync(
                c.Db,
                order,
                at,
                c.Now,
                c.Settings.PointsPerAmount,
                refundedAmounts,
                c.CancellationToken
            );

        c.Db.Orders.Add(order);
        await c.Db.SaveChangesAsync(c.CancellationToken);
        c.Db.ChangeTracker.Clear();
        c.Logger.LogInformation("Seeded order {OrderCode}.", order.OrderCode);
    }

    private static Task<bool> ExistsAsync(Context c, string code) =>
        c.Db.Orders.AnyAsync(o => o.OrderCode == code, c.CancellationToken);

    private static async Task<Dictionary<string, Combo>> LoadCombosAsync(
        AppDbContext db,
        CancellationToken cancellationToken
    ) =>
        (await db.Combos.AsNoTracking().Where(x => x.IsActive).ToListAsync(cancellationToken))
            .GroupBy(x => x.Name)
            .ToDictionary(g => g.Key, g => g.First());

    private static async Task<Dictionary<string, SeedBuyer>> LoadBuyersAsync(
        AppDbContext db,
        CancellationToken cancellationToken
    )
    {
        string[] emails =
        [
            SeedEmails.Customer1,
            SeedEmails.Customer2,
            SeedEmails.Customer3,
            SeedEmails.Customer4,
            SeedEmails.CustomerNew,
        ];
        var users = await db
            .Users.AsNoTracking()
            .Where(u => emails.Contains(u.Email) && u.IsActive)
            .Select(u => new
            {
                u.UserId,
                u.Email,
                u.FullName,
                u.Phone,
            })
            .ToListAsync(cancellationToken);
        return users.ToDictionary(u => u.Email, u => new SeedBuyer(u.UserId, u.FullName, u.Phone));
    }

    private static async Task<Dictionary<string, SeedStaff>> LoadStaffAsync(
        AppDbContext db,
        CancellationToken cancellationToken
    )
    {
        var staff = await db
            .Employees.AsNoTracking()
            .Where(e => e.IsActive && e.User.IsActive)
            .Select(e => new
            {
                e.EmployeeId,
                e.UserId,
                e.User.Email,
            })
            .ToListAsync(cancellationToken);
        return staff.ToDictionary(e => e.Email, e => new SeedStaff(e.EmployeeId, e.UserId));
    }
}
