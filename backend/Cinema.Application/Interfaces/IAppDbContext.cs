using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Cinema.Application.Interfaces;

public interface IAppDbContext
{
    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    DbSet<CinemaSettings> CinemaSettings { get; }
    DbSet<Role> Roles { get; }
    DbSet<User> Users { get; }
    DbSet<MembershipTier> MembershipTiers { get; }
    DbSet<CustomerProfile> CustomerProfiles { get; }
    DbSet<LoyaltyPointEntry> LoyaltyPointEntries { get; }
    DbSet<Position> Positions { get; }
    DbSet<Employee> Employees { get; }
    DbSet<ScreenType> ScreenTypes { get; }
    DbSet<CinemaRoom> CinemaRooms { get; }
    DbSet<RoomClosure> RoomClosures { get; }
    DbSet<ShowtimeRoomChange> ShowtimeRoomChanges { get; }
    DbSet<RoomChangeRefundDue> RoomChangeRefundDues { get; }
    DbSet<ShowtimeRoomChangeNotice> ShowtimeRoomChangeNotices { get; }
    DbSet<SeatType> SeatTypes { get; }
    DbSet<Seat> Seats { get; }
    DbSet<Genre> Genres { get; }
    DbSet<Language> Languages { get; }
    DbSet<AgeRating> AgeRatings { get; }
    DbSet<Director> Directors { get; }
    DbSet<Movie> Movies { get; }
    DbSet<MovieGenre> MovieGenres { get; }
    DbSet<Actor> Actors { get; }
    DbSet<MovieCast> MovieCast { get; }
    DbSet<Review> Reviews { get; }
    DbSet<Showtime> Showtimes { get; }
    DbSet<ShowtimeSeat> ShowtimeSeats { get; }
    DbSet<SeatBlock> SeatBlocks { get; }
    DbSet<Order> Orders { get; }
    DbSet<OrderLine> OrderLines { get; }
    DbSet<OrderPriceAdjustment> OrderPriceAdjustments { get; }
    DbSet<OrderPriceAdjustmentAllocation> OrderPriceAdjustmentAllocations { get; }
    DbSet<Combo> Combos { get; }
    DbSet<InventoryItem> InventoryItems { get; }
    DbSet<ComboRecipe> ComboRecipes { get; }
    DbSet<ComboRecipeLine> ComboRecipeLines { get; }
    DbSet<StockReceipt> StockReceipts { get; }
    DbSet<StockReceiptLine> StockReceiptLines { get; }
    DbSet<StockLedger> StockLedgers { get; }
    DbSet<InventoryReservation> InventoryReservations { get; }
    DbSet<ComboPickup> ComboPickups { get; }
    DbSet<StocktakeSheet> StocktakeSheets { get; }
    DbSet<StocktakeSheetLine> StocktakeSheetLines { get; }
    DbSet<WasteSheet> WasteSheets { get; }
    DbSet<WasteSheetLine> WasteSheetLines { get; }
    DbSet<TicketType> TicketTypes { get; }
    DbSet<PricingRule> PricingRules { get; }
    DbSet<SpecialDay> SpecialDays { get; }
    DbSet<Promotion> Promotions { get; }
    DbSet<PromotionRedemption> PromotionRedemptions { get; }
    DbSet<PromotionCustomerUsage> PromotionCustomerUsages { get; }
    DbSet<PaymentTransaction> PaymentTransactions { get; }
    DbSet<CounterShift> CounterShifts { get; }
    DbSet<DailyClosing> DailyClosings { get; }
    DbSet<LedgerAdjustment> LedgerAdjustments { get; }
    DbSet<GiftCard> GiftCards { get; }
    DbSet<GiftCardLedger> GiftCardLedgers { get; }
    DbSet<GiftCardRevenueEntry> GiftCardRevenueEntries { get; }
    DbSet<GiftCardAttemptLimit> GiftCardAttemptLimits { get; }
    DbSet<OrderResourceHold> OrderResourceHolds { get; }
    DbSet<OutboxMessage> OutboxMessages { get; }
    DbSet<OutboxDelivery> OutboxDeliveries { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<UserVerificationToken> UserVerificationTokens { get; }
    DbSet<ImportExportJob> ImportExportJobs { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<ShowtimeDailyStat> ShowtimeDailyStats { get; }
    DbSet<PaymentDailyStat> PaymentDailyStats { get; }
    DbSet<ShowtimeSalesPace> ShowtimeSalesPaces { get; }
    DbSet<ReconciliationSession> ReconciliationSessions { get; }
    DbSet<ReconciliationItem> ReconciliationItems { get; }
    DbSet<SeatHoldEvent> SeatHoldEvents { get; }
    DbSet<InvoiceProfile> InvoiceProfiles { get; }
    DbSet<InvoiceRequest> InvoiceRequests { get; }
}

public static class AppDbContextExtensions
{
    public const int CinemaSettingsId = 1;

    public static Task<CinemaSettings> GetCinemaSettingsAsync(
        this IAppDbContext db,
        CancellationToken cancellationToken
    ) => db.CinemaSettings.FirstAsync(s => s.Id == CinemaSettingsId, cancellationToken);

    public static async Task<CinemaSettings> LoadCinemaSettingsOrDefaultAsync(
        this IAppDbContext db,
        CancellationToken cancellationToken
    ) =>
        await db
            .CinemaSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == CinemaSettingsId, cancellationToken)
        ?? new CinemaSettings();

    public static async Task<int> LoadShowtimeLookaheadDaysAsync(
        this IAppDbContext db,
        CancellationToken cancellationToken
    ) =>
        await db
            .CinemaSettings.Where(s => s.Id == CinemaSettingsId)
            .Select(s => (int?)s.ShowtimeLookaheadDays)
            .FirstOrDefaultAsync(cancellationToken)
        ?? CinemaSettings.DefaultShowtimeLookaheadDays;
}
