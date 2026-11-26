import type { RouteObject } from "react-router";

import AdminLayout from "@/layouts/admin/admin-layout";
import AdminAuthLayout from "@/layouts/admin/admin-auth-layout";
import {
  AdminIndexRedirect,
  AdminProtectedRoute,
} from "./protected-route/admin-protected-route";
import { GuestRoute } from "./protected-route/guest-route";
import { ROUTES } from "@/config/routes";
import { lazyRoute } from "./lazy-route";

const AdminLoginPage = lazyRoute(() => import("@/features/admin/login/pages/login-page"));
const AdminResetPasswordPage = lazyRoute(() => import("@/features/admin/login/pages/reset-password-page"));
const AdminChangePasswordPage = lazyRoute(() => import("@/features/admin/account/pages/change-password-page"));
const AdminDashboardPage = lazyRoute(() => import("@/features/admin/dashboard/pages/dashboard-page"));
const AdminShowtimesPage = lazyRoute(() => import("@/features/admin/showtimes/pages/showtimes-page"));
const AdminScheduleAnalysisPage = lazyRoute(() => import("@/features/admin/schedule-analysis/pages/schedule-analysis-page"));
const AdminCounterPage = lazyRoute(() => import("@/features/admin/counter/pages/counter-page"));
const AdminCheckinPage = lazyRoute(() => import("@/features/admin/checkin/pages/checkin-page"));
const AdminAuditLogsPage = lazyRoute(() => import("@/features/admin/audit-logs/pages/audit-logs-page"));
const AdminCounterShiftsPage = lazyRoute(() => import("@/features/admin/counter-shifts/pages/counter-shifts-page"));
const AdminDailyClosingPage = lazyRoute(() => import("@/features/admin/daily-closings/pages/daily-closing-page"));
const AdminDailyClosingHistoryPage = lazyRoute(() => import("@/features/admin/daily-closings/pages/daily-closing-history-page"));
const AdminDailyClosingReportPage = lazyRoute(() => import("@/features/admin/daily-closings/pages/daily-closing-report-page"));
const AdminLedgerAdjustmentsPage = lazyRoute(() => import("@/features/admin/ledger-adjustments/pages/ledger-adjustments-page"));
const AdminGiftCardsPage = lazyRoute(() => import("@/features/admin/gift-cards/pages/gift-cards-page"));
const AdminCashRefundsPage = lazyRoute(() => import("@/features/admin/cash-refunds/pages/cash-refunds-page"));
const AdminRoomChangeRefundsPage = lazyRoute(
  () => import("@/features/admin/room-change-refunds/pages/room-change-refunds-page"),
);
const AdminUsersPage = lazyRoute(() => import("@/features/admin/users/pages/users-page"));
const AdminInvoiceRequestsPage = lazyRoute(() => import("@/features/admin/invoices/pages/invoice-requests-page"));
const AdminSupportRequestsPage = lazyRoute(() => import("@/features/admin/support/pages/support-requests-page"));
const AdminSupportRequestDetailPage = lazyRoute(() => import("@/features/admin/support/pages/support-request-detail-page"));
const AdminRefundProposalsPage = lazyRoute(() => import("@/features/admin/support/pages/refund-proposals-page"));
const AdminTaxReportPage = lazyRoute(() => import("@/features/admin/reports/pages/tax-report-page"));
const AdminFinanceReportPage = lazyRoute(() => import("@/features/admin/reports/pages/finance-report-page"));
const AdminReconciliationPage = lazyRoute(() => import("@/features/admin/reconciliation/pages/reconciliation-page"));
const AdminReconciliationDetailPage = lazyRoute(() => import("@/features/admin/reconciliation/pages/reconciliation-detail-page"));
const AdminReportsPage = lazyRoute(() => import("@/features/admin/reports/pages/reports.page"));
const AdminRoomsPage = lazyRoute(() => import("@/features/admin/rooms/pages/rooms-page"));
const AdminEmployeesPage = lazyRoute(() => import("@/features/admin/employees/pages/employees-page"));
const AdminRoomSeatsPage = lazyRoute(() => import("@/features/admin/rooms/pages/room-seats-page"));
const AdminRoomClosuresPage = lazyRoute(() => import("@/features/admin/room-closures/pages/room-closures-page"));
const AdminRoomClosureDetailPage = lazyRoute(() => import("@/features/admin/room-closures/pages/room-closure-detail-page"));
const AdminRoomRentalsPage = lazyRoute(() => import("@/features/admin/room-rentals/pages/room-rental-requests-page"));
const AdminRoomRentalDetailPage = lazyRoute(() => import("@/features/admin/room-rentals/pages/room-rental-request-detail-page"));
const AdminRoomRentalCalendarPage = lazyRoute(() => import("@/features/admin/room-rentals/pages/room-rental-calendar-page"));
const AdminRoomRentalReportPage = lazyRoute(() => import("@/features/admin/room-rentals/pages/room-rental-report-page"));
const AdminRoomRentalRatesPage = lazyRoute(() => import("@/features/admin/room-rentals/pages/room-rental-rates-page"));
const AdminCombosPage = lazyRoute(() => import("@/features/admin/combos/pages/combos-page"));
const AdminInventoryStockPage = lazyRoute(() => import("@/features/admin/inventory/pages/inventory-stock-page"));
const AdminInventoryItemsPage = lazyRoute(() => import("@/features/admin/inventory/pages/inventory-items-page"));
const AdminComboRecipesPage = lazyRoute(() => import("@/features/admin/inventory/pages/combo-recipes-page"));
const AdminStockReceiptsPage = lazyRoute(() => import("@/features/admin/inventory/pages/stock-receipts-page"));
const AdminStocktakesPage = lazyRoute(() => import("@/features/admin/inventory/pages/stocktakes-page"));
const AdminWasteSheetsPage = lazyRoute(() => import("@/features/admin/inventory/pages/waste-sheets-page"));
const AdminInventoryLedgerPage = lazyRoute(() => import("@/features/admin/inventory/pages/inventory-ledger-page"));
const AdminInventoryValuationPage = lazyRoute(() => import("@/features/admin/inventory/pages/inventory-valuation-page"));
const AdminComboPickupPage = lazyRoute(() => import("@/features/admin/combo-pickup/pages/combo-pickup-page"));
const AdminUnclaimedComboOrdersPage = lazyRoute(() => import("@/features/admin/inventory/pages/unclaimed-combo-orders-page"));
const AdminReviewsPage = lazyRoute(() => import("@/features/admin/reviews/pages/reviews-page"));
const AdminCustomerRestrictionsPage = lazyRoute(
  () => import("@/features/admin/customer-restrictions/pages/customer-restrictions-page"),
);
const AdminImportExportHistoryPage = lazyRoute(() => import("@/features/admin/import-export/pages/import-export-history-page"));
const AdminSettingsPage = lazyRoute(() => import("@/features/admin/settings/pages/settings-page"));
const CatalogHubPage = lazyRoute(() => import("@/features/admin/catalog/pages/catalog-hub-page"));
const GenresPage = lazyRoute(() => import("@/features/admin/catalog/pages/genres-page"));
const LanguagesPage = lazyRoute(() => import("@/features/admin/catalog/pages/languages-page"));
const AgeRatingsPage = lazyRoute(() => import("@/features/admin/catalog/pages/age-ratings-page"));
const SeatTypesPage = lazyRoute(() => import("@/features/admin/catalog/pages/seat-types-page"));
const ScreenTypesPage = lazyRoute(() => import("@/features/admin/catalog/pages/screen-types-page"));
const MembershipTiersPage = lazyRoute(() => import("@/features/admin/catalog/pages/membership-tiers-page"));
const PositionsPage = lazyRoute(() => import("@/features/admin/catalog/pages/positions-page"));
const ActorsPage = lazyRoute(() => import("@/features/admin/catalog/pages/actors-page"));
const DirectorsPage = lazyRoute(() => import("@/features/admin/catalog/pages/directors-page"));
const AdminMoviesPage = lazyRoute(() => import("@/features/admin/movies/pages/movies-page"));
const AdminPromotionsPage = lazyRoute(() => import("@/features/admin/promotions/pages/promotions-page"));
const AdminPromotionReportsPage = lazyRoute(() => import("@/features/admin/promotions/pages/promotion-reports-page"));
const AdminPromotionReportDetailPage = lazyRoute(() => import("@/features/admin/promotions/pages/promotion-report-detail-page"));
const AdminPromotionRedemptionsPage = lazyRoute(() => import("@/features/admin/promotions/pages/promotion-redemptions-page"));
const AdminLoyaltyRewardsPage = lazyRoute(() => import("@/features/admin/loyalty/pages/loyalty-rewards-page"));
const AdminLoyaltyChallengesPage = lazyRoute(() => import("@/features/admin/loyalty/pages/loyalty-challenges-page"));
const AdminLoyaltyAdjustmentsPage = lazyRoute(() => import("@/features/admin/loyalty/pages/loyalty-adjustments-page"));
const AdminLoyaltyReportPage = lazyRoute(() => import("@/features/admin/loyalty/pages/loyalty-report-page"));
const AdminTicketTypesPage = lazyRoute(() => import("@/features/admin/pricing/pages/ticket-types-page"));
const AdminPricingRulesPage = lazyRoute(() => import("@/features/admin/pricing/pages/pricing-rules-page"));
const AdminPricingRuleHistoryPage = lazyRoute(() => import("@/features/admin/pricing/pages/pricing-rule-history-page"));
const AdminSpecialDaysPage = lazyRoute(() => import("@/features/admin/pricing/pages/special-days-page"));
const AdminPricePreviewPage = lazyRoute(() => import("@/features/admin/pricing/pages/price-preview-page"));
const AdminOrdersPage = lazyRoute(() => import("@/features/admin/orders/pages/orders-page"));
const AdminOrderDetailPage = lazyRoute(() => import("@/features/admin/orders/pages/order-detail-page"));
const AdminOrderExchangePage = lazyRoute(() => import("@/features/admin/orders/pages/order-exchange-page"));

export const adminRoutes: RouteObject[] = [
  {
    path: ROUTES.admin.login,
    element: <GuestRoute redirectTo={ROUTES.admin.dashboard} />,
    children: [
      {
        element: <AdminAuthLayout />,
        children: [{ index: true, lazy: AdminLoginPage }],
      },
    ],
  },
  {
    path: ROUTES.admin.resetPassword,
    element: <AdminAuthLayout />,
    children: [{ index: true, lazy: AdminResetPasswordPage }],
  },
  {
    path: ROUTES.admin.root,
    element: <AdminProtectedRoute />,
    children: [
      { index: true, element: <AdminIndexRedirect /> },
      {
        path: "change-password",
        element: <AdminAuthLayout />,
        children: [{ index: true, lazy: AdminChangePasswordPage }],
      },
      {
        element: <AdminLayout />,
        children: [
          { path: "dashboard", lazy: AdminDashboardPage },
          { path: "movies", lazy: AdminMoviesPage },
          { path: "showtimes", lazy: AdminShowtimesPage },
          { path: "schedule-analysis", lazy: AdminScheduleAnalysisPage },
          { path: "orders", lazy: AdminOrdersPage },
          { path: "orders/:id", lazy: AdminOrderDetailPage },
          { path: "orders/:id/exchange", lazy: AdminOrderExchangePage },
          { path: "bookings/counter", lazy: AdminCounterPage },
          { path: "checkin", lazy: AdminCheckinPage },
          { path: "counter-shifts", lazy: AdminCounterShiftsPage },
          { path: "daily-closings", lazy: AdminDailyClosingPage },
          { path: "daily-closings/history", lazy: AdminDailyClosingHistoryPage },
          { path: "daily-closings/:date/report", lazy: AdminDailyClosingReportPage },
          { path: "ledger-adjustments", lazy: AdminLedgerAdjustmentsPage },
          { path: "gift-cards", lazy: AdminGiftCardsPage },
          { path: "cash-refunds", lazy: AdminCashRefundsPage },
          { path: "room-change-refunds", lazy: AdminRoomChangeRefundsPage },
          { path: "audit-logs", lazy: AdminAuditLogsPage },
          { path: "reports", lazy: AdminReportsPage },
          { path: "reports/finance", lazy: AdminFinanceReportPage },
          { path: "reports/tax", lazy: AdminTaxReportPage },
          { path: "invoice-requests", lazy: AdminInvoiceRequestsPage },
          { path: "support-requests", lazy: AdminSupportRequestsPage },
          { path: "support-requests/:id", lazy: AdminSupportRequestDetailPage },
          { path: "refund-proposals", lazy: AdminRefundProposalsPage },
          { path: "reconciliation", lazy: AdminReconciliationPage },
          { path: "reconciliation/:id", lazy: AdminReconciliationDetailPage },
          { path: "users", lazy: AdminUsersPage },
          { path: "employees", lazy: AdminEmployeesPage },
          { path: "rooms", lazy: AdminRoomsPage },
          { path: "rooms/:id/seats", lazy: AdminRoomSeatsPage },
          { path: "room-closures", lazy: AdminRoomClosuresPage },
          { path: "room-closures/:id", lazy: AdminRoomClosureDetailPage },
          { path: "room-rentals", lazy: AdminRoomRentalsPage },
          { path: "room-rentals/rates", lazy: AdminRoomRentalRatesPage },
          { path: "room-rentals/calendar", lazy: AdminRoomRentalCalendarPage },
          { path: "room-rentals/report", lazy: AdminRoomRentalReportPage },
          { path: "room-rentals/:id", lazy: AdminRoomRentalDetailPage },
          { path: "combos", lazy: AdminCombosPage },
          { path: "inventory", lazy: AdminInventoryStockPage },
          { path: "inventory/items", lazy: AdminInventoryItemsPage },
          { path: "inventory/recipes", lazy: AdminComboRecipesPage },
          { path: "inventory/receipts", lazy: AdminStockReceiptsPage },
          { path: "inventory/stocktakes", lazy: AdminStocktakesPage },
          { path: "inventory/waste", lazy: AdminWasteSheetsPage },
          { path: "inventory/ledger", lazy: AdminInventoryLedgerPage },
          { path: "inventory/valuation", lazy: AdminInventoryValuationPage },
          { path: "combo-pickups", lazy: AdminComboPickupPage },
          { path: "combo-pickups/unclaimed", lazy: AdminUnclaimedComboOrdersPage },
          { path: "promotions", lazy: AdminPromotionsPage },
          { path: "promotions/reports", lazy: AdminPromotionReportsPage },
          { path: "promotions/:id/report", lazy: AdminPromotionReportDetailPage },
          { path: "promotions/:id/redemptions", lazy: AdminPromotionRedemptionsPage },
          { path: "loyalty/rewards", lazy: AdminLoyaltyRewardsPage },
          { path: "loyalty/challenges", lazy: AdminLoyaltyChallengesPage },
          { path: "loyalty/adjustments", lazy: AdminLoyaltyAdjustmentsPage },
          { path: "loyalty/report", lazy: AdminLoyaltyReportPage },
          { path: "pricing/ticket-types", lazy: AdminTicketTypesPage },
          { path: "pricing/rules", lazy: AdminPricingRulesPage },
          { path: "pricing/rule-history", lazy: AdminPricingRuleHistoryPage },
          { path: "pricing/special-days", lazy: AdminSpecialDaysPage },
          { path: "pricing/preview", lazy: AdminPricePreviewPage },
          { path: "reviews", lazy: AdminReviewsPage },
          { path: "customer-restrictions", lazy: AdminCustomerRestrictionsPage },
          { path: "import-export", lazy: AdminImportExportHistoryPage },
          { path: "settings", lazy: AdminSettingsPage },
          { path: "catalog", lazy: CatalogHubPage },
          { path: "catalog/genres", lazy: GenresPage },
          { path: "catalog/languages", lazy: LanguagesPage },
          { path: "catalog/age-ratings", lazy: AgeRatingsPage },
          { path: "catalog/seat-types", lazy: SeatTypesPage },
          { path: "catalog/screen-types", lazy: ScreenTypesPage },
          {
            path: "catalog/membership-tiers",
            lazy: MembershipTiersPage,
          },
          { path: "catalog/positions", lazy: PositionsPage },
          { path: "catalog/actors", lazy: ActorsPage },
          { path: "catalog/directors", lazy: DirectorsPage },
        ],
      },
    ],
  },
];
