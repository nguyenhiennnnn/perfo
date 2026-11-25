using Cinema.Application.Interfaces;
using Cinema.Domain.Entities;
using Cinema.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Cinema.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options),
        IAppDbContext
{
    DatabaseFacade IAppDbContext.Database => Database;

    public DbSet<CinemaSettings> CinemaSettings => Set<CinemaSettings>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<MembershipTier> MembershipTiers => Set<MembershipTier>();
    public DbSet<CustomerProfile> CustomerProfiles => Set<CustomerProfile>();
    public DbSet<LoyaltyPointEntry> LoyaltyPointEntries => Set<LoyaltyPointEntry>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<ScreenType> ScreenTypes => Set<ScreenType>();
    public DbSet<CinemaRoom> CinemaRooms => Set<CinemaRoom>();
    public DbSet<RoomClosure> RoomClosures => Set<RoomClosure>();
    public DbSet<ShowtimeRoomChange> ShowtimeRoomChanges => Set<ShowtimeRoomChange>();
    public DbSet<RoomChangeRefundDue> RoomChangeRefundDues => Set<RoomChangeRefundDue>();
    public DbSet<ShowtimeRoomChangeNotice> ShowtimeRoomChangeNotices => Set<ShowtimeRoomChangeNotice>();
    public DbSet<SeatType> SeatTypes => Set<SeatType>();
    public DbSet<Seat> Seats => Set<Seat>();
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<Language> Languages => Set<Language>();
    public DbSet<AgeRating> AgeRatings => Set<AgeRating>();
    public DbSet<Director> Directors => Set<Director>();
    public DbSet<Movie> Movies => Set<Movie>();
    public DbSet<MovieGenre> MovieGenres => Set<MovieGenre>();
    public DbSet<Actor> Actors => Set<Actor>();
    public DbSet<MovieCast> MovieCast => Set<MovieCast>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Showtime> Showtimes => Set<Showtime>();
    public DbSet<ShowtimeSeat> ShowtimeSeats => Set<ShowtimeSeat>();
    public DbSet<SeatBlock> SeatBlocks => Set<SeatBlock>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    public DbSet<OrderPriceAdjustment> OrderPriceAdjustments => Set<OrderPriceAdjustment>();
    public DbSet<OrderPriceAdjustmentAllocation> OrderPriceAdjustmentAllocations =>
        Set<OrderPriceAdjustmentAllocation>();
    public DbSet<Combo> Combos => Set<Combo>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<ComboRecipe> ComboRecipes => Set<ComboRecipe>();
    public DbSet<ComboRecipeLine> ComboRecipeLines => Set<ComboRecipeLine>();
    public DbSet<StockReceipt> StockReceipts => Set<StockReceipt>();
    public DbSet<StockReceiptLine> StockReceiptLines => Set<StockReceiptLine>();
    public DbSet<StockLedger> StockLedgers => Set<StockLedger>();
    public DbSet<InventoryReservation> InventoryReservations => Set<InventoryReservation>();
    public DbSet<ComboPickup> ComboPickups => Set<ComboPickup>();
    public DbSet<StocktakeSheet> StocktakeSheets => Set<StocktakeSheet>();
    public DbSet<StocktakeSheetLine> StocktakeSheetLines => Set<StocktakeSheetLine>();
    public DbSet<WasteSheet> WasteSheets => Set<WasteSheet>();
    public DbSet<WasteSheetLine> WasteSheetLines => Set<WasteSheetLine>();
    public DbSet<TicketType> TicketTypes => Set<TicketType>();
    public DbSet<PricingRule> PricingRules => Set<PricingRule>();
    public DbSet<SpecialDay> SpecialDays => Set<SpecialDay>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<PromotionRedemption> PromotionRedemptions => Set<PromotionRedemption>();
    public DbSet<PromotionCustomerUsage> PromotionCustomerUsages => Set<PromotionCustomerUsage>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<OrderResourceHold> OrderResourceHolds => Set<OrderResourceHold>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<OutboxDelivery> OutboxDeliveries => Set<OutboxDelivery>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserVerificationToken> UserVerificationTokens => Set<UserVerificationToken>();
    public DbSet<ImportExportJob> ImportExportJobs => Set<ImportExportJob>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ShowtimeDailyStat> ShowtimeDailyStats => Set<ShowtimeDailyStat>();
    public DbSet<PaymentDailyStat> PaymentDailyStats => Set<PaymentDailyStat>();
    public DbSet<ShowtimeSalesPace> ShowtimeSalesPaces => Set<ShowtimeSalesPace>();
    public DbSet<ReconciliationSession> ReconciliationSessions => Set<ReconciliationSession>();
    public DbSet<ReconciliationItem> ReconciliationItems => Set<ReconciliationItem>();
    public DbSet<SeatHoldEvent> SeatHoldEvents => Set<SeatHoldEvent>();
    public DbSet<InvoiceProfile> InvoiceProfiles => Set<InvoiceProfile>();
    public DbSet<InvoiceRequest> InvoiceRequests => Set<InvoiceRequest>();
    public DbSet<CounterShift> CounterShifts => Set<CounterShift>();
    public DbSet<DailyClosing> DailyClosings => Set<DailyClosing>();
    public DbSet<LedgerAdjustment> LedgerAdjustments => Set<LedgerAdjustment>();
    public DbSet<GiftCard> GiftCards => Set<GiftCard>();
    public DbSet<GiftCardLedger> GiftCardLedgers => Set<GiftCardLedger>();
    public DbSet<GiftCardRevenueEntry> GiftCardRevenueEntries => Set<GiftCardRevenueEntry>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<NullableUtcDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        SeedData.Apply(modelBuilder);
    }
}
