using Cinema.Application.DTOs.Movies;
using Cinema.Application.DTOs.Promotions;
using Cinema.Application.DTOs.Showtimes;
using Cinema.Application.Caching;
using Cinema.Application.Events;
using Cinema.Application.Interfaces;
using Cinema.Application.Services.Ai;
using Cinema.Application.Services.Audit;
using Cinema.Application.Services.Auth;
using Cinema.Application.Services.Catalog;
using Cinema.Application.Services.Combos;
using Cinema.Application.Services.Inventory;
using Cinema.Application.Services.Dashboard;
using Cinema.Application.Services.Employees;
using Cinema.Application.Services.Events;
using Cinema.Application.Services.Import;
using Cinema.Application.Services.ImportExport;
using Cinema.Application.Services.Movies;
using Cinema.Application.Services.Notifications;
using Cinema.Application.Services.Orders;
using Cinema.Application.Services.Payments;
using Cinema.Application.Services.Pricing;
using Cinema.Application.Services.Promotions;
using Cinema.Application.Services.Reports;
using Cinema.Application.Services.Reviews;
using Cinema.Application.Services.Rooms;
using Cinema.Application.Services.Settings;
using Cinema.Application.Services.Showtimes;
using Cinema.Application.Services.Users;
using Cinema.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Cinema.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IRegistrationService, RegistrationService>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<IVerificationTokenSender, VerificationTokenSender>();
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<IMovieService, MovieService>();
        services.AddMemoryCache();
        services.AddSingleton<IAppCache, AppCache>();
        services.AddCinemaCaches();
        services.AddScoped<IAuditLogger, AuditLogger>();
        services.AddScoped<IAuditLogQueryService, AuditLogQueryService>();
        services.AddSingleton<IImportSessionStore, ImportSessionStore>();
        services.AddScoped<IImportProcessor<MovieImportRow>, MovieImportProcessor>();
        services.AddScoped<IImportProcessor<ShowtimeImportRow>, ShowtimeImportProcessor>();
        services.AddScoped(typeof(IImportService<>), typeof(ImportService<>));
        services.AddScoped<IImportApplyModule, ImportApplyModule<MovieImportRow>>();
        services.AddScoped<IImportApplyModule, ImportApplyModule<ShowtimeImportRow>>();
        services.AddSingleton<IImportExportConcurrencyGuard, ImportExportConcurrencyGuard>();
        services.AddScoped<IImportExportJobTracker, ImportExportJobTracker>();
        services.AddScoped<IImportExportJobService, ImportExportJobService>();
        services.AddScoped<IExportJobService, ExportJobService>();
        services.AddScoped<IExportModule>(sp => new ExportModule<AdminShowtimeListFilter>(
            sp.GetRequiredService<IShowtimeExportService>()
        ));
        services.AddScoped<IExportModule>(sp => new ExportModule<OrderListFilter>(
            sp.GetRequiredService<IOrderExportService>()
        ));
        services.AddScoped<IExportModule>(sp => new ExportModule<AdminPaymentListFilter>(
            sp.GetRequiredService<IPaymentExportService>()
        ));
        services.AddScoped<IExportModule>(sp => new ExportModule<PromotionRedemptionListFilter>(
            sp.GetRequiredService<IPromotionRedemptionExportService>()
        ));
        services.AddScoped<IRoomService, RoomService>();
        services.AddScoped<ISeatLayoutImportService, SeatLayoutImportService>();
        services.AddScoped<ISeatExpirationService, SeatExpirationService>();
        services.AddScoped<IShowtimeService, ShowtimeService>();
        services.AddScoped<IShowtimeCancellationService, ShowtimeCancellationService>();
        services.AddScoped<IShowtimeBatchService, ShowtimeBatchService>();
        services.AddScoped<IShowtimeAutoScheduleService, ShowtimeAutoScheduleService>();
        services.AddScoped<IShowtimeQueryService, ShowtimeQueryService>();
        services.AddScoped<IShowtimeExportService, ShowtimeExportService>();
        services.AddScoped<IShowtimeLifecycleService, ShowtimeLifecycleService>();
        services.AddScoped<IBusinessEventDispatcher, BusinessEventDispatcher>();
        services.AddScoped<IOutboxProcessor, OutboxProcessor>();
        services.AddScoped<IOrderHoldPolicy, OnlineHoldPolicy>();
        services.AddScoped<IOrderHoldPolicy, CounterHoldPolicy>();
        services.AddScoped<IOrderResourceHoldService, OrderResourceHoldService>();
        services.AddScoped<PromotionResourceHold>();
        services.AddResourceHold<PromotionResourceHold>();
        services.AddScoped<IOrderPromotionService, OrderPromotionService>();
        services.AddScoped<IOrderLoyaltyService, OrderLoyaltyService>();
        services.AddScoped<IOrderRefundService, OrderRefundService>();
        services.AddScoped<IOrderLineRefundService, OrderLineRefundService>();
        services.AddScoped<IOrderReleaseService, OrderReleaseService>();
        services.AddScoped<IOrderCancellationFinalizer, OrderCancellationFinalizer>();
        services.AddScoped<IOrderCancellationService, OrderCancellationService>();
        services.AddScoped<IOrderConfirmationService, OrderConfirmationService>();
        services.AddBackgroundEventHandler<OrderConfirmed, OrderConfirmedEmailHandler>();
        services.AddBackgroundEventHandler<OrderCancelled, OrderCancelledEmailHandler>();
        services.AddBackgroundEventHandler<OrderCancelled, ShowtimeCancelledEmailHandler>();
        services.AddBackgroundEventHandler<OrderRefunded, ShowtimeRefundedEmailHandler>();
        services.AddScoped<IOrderQueryService, OrderQueryService>();
        services.AddScoped<IOrderExportService, OrderExportService>();
        services.AddScoped<IOrderTicketPdfService, OrderTicketPdfService>();
        services.AddScoped<IOrderTicketPricer, OrderTicketPricer>();
        services.AddScoped<IOrderHoldService, OrderHoldService>();
        services.AddScoped<IOrderCheckoutService, OrderCheckoutService>();
        services.AddScoped<ICounterOrderService, CounterOrderService>();
        services.AddScoped<ICounterPaymentService, CounterPaymentService>();
        services.AddScoped<ITicketCheckInService, TicketCheckInService>();
        services.AddScoped<ICardCheckService, CardCheckService>();
        services.AddScoped<ISupplementOrderService, SupplementOrderService>();
        services.AddInTransactionEventHandler<OrderConfirmed, SupplementCardCheckHandler>();
        services.AddInTransactionEventHandler<OrderCancelled, SupplementCardCheckRevertHandler>();
        services.AddScoped<LoyaltyExpiryService>();
        services.AddScoped<IComboService, ComboService>();
        services.AddScoped<IInventoryItemService, InventoryItemService>();
        services.AddScoped<IComboRecipeService, ComboRecipeService>();
        services.AddScoped<IStockLedgerService, StockLedgerService>();
        services.AddScoped<IStockReceiptService, StockReceiptService>();
        services.AddScoped<IStocktakeService, StocktakeService>();
        services.AddScoped<IWasteService, WasteService>();
        services.AddScoped<IInventoryStockQueryService, InventoryStockQueryService>();
        services.AddScoped<ITicketTypeService, TicketTypeService>();
        services.AddScoped<IPricingRuleService, PricingRuleService>();
        services.AddScoped<ISpecialDayService, SpecialDayService>();
        services.AddScoped<IPricingService, PricingService>();
        services.AddScoped<IPromotionService, PromotionService>();
        services.AddScoped<IPromotionEngine, PromotionEngine>();
        services.AddScoped<IMyPromotionService, MyPromotionService>();
        services.AddScoped<IPromotionReportService, PromotionReportService>();
        services.AddScoped<IPromotionRedemptionService, PromotionRedemptionService>();
        services.AddScoped<IPromotionRedemptionExportService, PromotionRedemptionExportService>();
        services.AddScoped<IPromotionBulkCodeService, PromotionBulkCodeService>();
        services.AddScoped<IMembershipBenefitPricer, NullMembershipBenefitPricer>();
        services.AddScoped<IOrderPriceCalculator, OrderPriceCalculator>();
        services.AddSingleton<IShowtimePricingScopeResolver, StandardShowtimePricingScopeResolver>();
        services.AddSingleton<ISalesVelocityProvider, NullSalesVelocityProvider>();
        services.AddScoped<IPaymentLedgerService, PaymentLedgerService>();
        services.AddScoped<IPaymentRefundService, PaymentRefundService>();
        services.AddScoped<ICashRefundService, CashRefundService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IPaymentAdminService, PaymentAdminService>();
        services.AddScoped<IPaymentExportService, PaymentExportService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<IUserProfileService, UserProfileService>();
        services.AddScoped<IReferralCodeService, ReferralCodeService>();
        services.AddScoped<IUnsubscribeTokenService, UnsubscribeTokenService>();
        services.AddScoped<ICommunicationPreferenceService, CommunicationPreferenceService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IReportExportService, ReportExportService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IUserAdminService, UserAdminService>();
        services.AddScoped<AiToolExecutor>();
        services.AddScoped<AiSystemPromptBuilder>();
        services.AddScoped<IAiAssistantService, AiAssistantService>();
        return services;
    }
}
