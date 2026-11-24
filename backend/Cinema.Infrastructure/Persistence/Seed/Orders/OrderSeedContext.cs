using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Cinema.Infrastructure.Persistence.Seed.Orders;

internal sealed record OnlineSpec(
    string Code,
    string CustomerEmail,
    SeedWhen When,
    string? ScreenType,
    int Seats,
    string? ComboName,
    int ComboQuantity,
    Func<SeedShowtime, DateTime, DateTime> CreatedAt
);

internal sealed record CounterSpec(
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

internal sealed record SeedDraft(
    Order Order,
    SeedSeatAllocation Allocation,
    IReadOnlyList<SeedTicket> Tickets,
    SeedBuyer? Buyer,
    SeedStaff? Staff
);

internal sealed class OrderSeedContext
{
    private readonly OrderSeedPool _pool;
    private readonly CancellationToken _token;

    private OrderSeedContext(
        AppDbContext db,
        ILogger logger,
        DateTime now,
        CinemaSettings settings,
        OrderSeedPool pool,
        IReadOnlyDictionary<string, Combo> combos,
        IReadOnlyDictionary<string, SeedBuyer> buyers,
        IReadOnlyDictionary<string, SeedStaff> staff,
        int? adminUserId,
        CancellationToken token
    )
    {
        Db = db;
        Logger = logger;
        Now = now;
        Settings = settings;
        _pool = pool;
        Combos = combos;
        Buyers = buyers;
        Staff = staff;
        AdminUserId = adminUserId;
        _token = token;
    }

    public AppDbContext Db { get; }

    public ILogger Logger { get; }

    public DateTime Now { get; }

    public CinemaSettings Settings { get; }

    public IReadOnlyDictionary<string, Combo> Combos { get; }

    public IReadOnlyDictionary<string, SeedBuyer> Buyers { get; }

    public IReadOnlyDictionary<string, SeedStaff> Staff { get; }

    public int? AdminUserId { get; }

    public CancellationToken Token => _token;

    public static async Task<OrderSeedContext?> CreateAsync(
        SeedContext context,
        CancellationToken cancellationToken
    )
    {
        if (!context.SeedDemoScenarios)
            return null;

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

        return new OrderSeedContext(
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
    }

    public Task<bool> ExistsAsync(string code) =>
        Db.Orders.AnyAsync(o => o.OrderCode == code, _token);

    public async Task<SeedDraft?> DraftOnlineAsync(OnlineSpec spec)
    {
        if (!Buyers.TryGetValue(spec.CustomerEmail, out var buyer))
        {
            Logger.LogWarning(
                "Skip seeding order {OrderCode}: customer {Email} not found.",
                spec.Code,
                spec.CustomerEmail
            );
            return null;
        }

        if (!TryResolveCombo(spec.Code, spec.ComboName, out var combo))
            return null;

        var allocation = await AllocateAsync(spec.Code, spec.When, spec.ScreenType, spec.Seats);
        if (allocation is null)
            return null;

        var order = OrderSeedKit.NewOrder(
            spec.Code,
            OrderType.Ticket,
            SalesChannel.Online,
            spec.CreatedAt(allocation.Showtime, Now),
            Settings.HoldDurationMinutes,
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
            await OrderSeedKit.ResolveTierAsync(Db, buyer.UserId, Now, _token)
        );
        OrderSeedKit.FinalizeAmounts(order);

        return new SeedDraft(order, allocation, tickets, buyer, null);
    }

    public async Task<SeedDraft?> DraftCounterAsync(CounterSpec spec)
    {
        if (!Staff.TryGetValue(spec.StaffEmail, out var staff))
        {
            Logger.LogWarning(
                "Skip seeding order {OrderCode}: staff {Email} not found.",
                spec.Code,
                spec.StaffEmail
            );
            return null;
        }

        if (!TryResolveCombo(spec.Code, spec.ComboName, out var combo))
            return null;

        var allocation = await AllocateAsync(spec.Code, spec.When, spec.ScreenType, spec.Seats);
        if (allocation is null)
            return null;

        var order = OrderSeedKit.NewOrder(
            spec.Code,
            OrderType.Ticket,
            SalesChannel.Counter,
            Now - spec.Age,
            Settings.HoldDurationMinutes,
            null,
            spec.CustomerName,
            spec.CustomerPhone,
            staff.EmployeeId
        );
        var tickets = OrderSeedKit.AddTickets(order, allocation);
        if (combo is not null)
            OrderSeedKit.AddCombo(order, combo, spec.ComboQuantity, allocation.Showtime.ShowtimeId);

        OrderSeedKit.FinalizeAmounts(order);

        return new SeedDraft(order, allocation, tickets, null, staff);
    }

    public async Task SaveAsync(
        Order order,
        DateTime? confirmedAt,
        IReadOnlyList<decimal> refundedAmounts,
        IReadOnlyList<SeedTicket>? heldTickets = null
    )
    {
        if (confirmedAt is { } at)
            await OrderSeedKit.ApplyLoyaltyAsync(
                Db,
                order,
                at,
                Now,
                Settings.PointsPerAmount,
                refundedAmounts,
                _token
            );

        Db.Orders.Add(order);
        try
        {
            await Db.SaveChangesAsync(_token);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException { SqlState: "23505" } pgEx
                  && pgEx.ConstraintName == "IX_Orders_OrderCode")
        {
            Db.ChangeTracker.Clear();
            Logger.LogWarning(
                "Order {OrderCode} already exists in the database – skipping (duplicate key).",
                order.OrderCode
            );
            return;
        }

        if (heldTickets is { Count: > 0 })
        {
            foreach (var ticket in heldTickets)
                ticket.Seat.HeldByOrderId = order.OrderId;

            await Db.SaveChangesAsync(_token);
        }

        Db.ChangeTracker.Clear();
        Logger.LogInformation("Seeded order {OrderCode}.", order.OrderCode);
    }

    public async Task SeedCancelledOnlineAsync(
        OnlineSpec spec,
        CancelReason reason,
        CancelledBy cancelledBy,
        DateTime cancelledAt,
        decimal refundPercent,
        int? performedByUserId,
        string note
    )
    {
        if (await ExistsAsync(spec.Code))
            return;

        var draft = await DraftOnlineAsync(spec);
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
        OrderSeedKit.IssueTickets(order, draft.Tickets, keepSeatsBooked: false);
        OrderSeedKit.AddComboHolds(order, ResourceHoldStatus.Committed, paidAt);

        var refunds = new List<decimal>();
        var refundAmount = Math.Min(
            order.FinalAmount,
            Cinema.Application.Utils.OrderPricing.RoundToDong(
                order.FinalAmount * refundPercent / 100m
            )
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

        await SaveAsync(order, paidAt, refunds);
    }

    public static string CancelOrderNote(string code) =>
        string.Format(Messages.Refund_CancelOrderDescription, code);

    public static string ShowtimeCancelledNote(string code) =>
        string.Format(Messages.Refund_ShowtimeCancelledDescription, code);

    private bool TryResolveCombo(string orderCode, string? comboName, out Combo? combo)
    {
        combo = null;
        if (comboName is null)
            return true;

        if (Combos.TryGetValue(comboName, out combo))
            return true;

        Logger.LogWarning(
            "Skip seeding order {OrderCode}: combo {Combo} not found.",
            orderCode,
            comboName
        );
        return false;
    }

    private async Task<SeedSeatAllocation?> AllocateAsync(
        string orderCode,
        SeedWhen when,
        string? screenType,
        int seats
    )
    {
        var allocation = await _pool.AllocateAsync(Db, when, screenType, seats, _token);
        if (allocation is null)
            Logger.LogWarning(
                "Skip seeding order {OrderCode}: no suitable showtime with free seats.",
                orderCode
            );

        return allocation;
    }

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
