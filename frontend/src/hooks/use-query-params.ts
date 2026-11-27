import {
  debounce,
  parseAsArrayOf,
  parseAsInteger,
  parseAsString,
  parseAsStringLiteral,
  useQueryStates,
} from "nuqs";
import type { MovieStatus, OccupancyGroupBy, ShowtimeKind } from "@/types/enum";
import {
  DEFAULT_MOVIES_PAGE_LIMIT,
  DEFAULT_PAGE,
  DEFAULT_PAGE_SIZE,
  SEARCH_DEBOUNCE_MS,
} from "@/config/default-constants";

const searchParser = parseAsString.withDefault("").withOptions({
  limitUrlUpdates: debounce(SEARCH_DEBOUNCE_MS),
  shallow: false,
});

const adminListParsers = {
  page: parseAsInteger.withDefault(DEFAULT_PAGE),
  pageSize: parseAsInteger.withDefault(DEFAULT_PAGE_SIZE),
  search: searchParser,
};

export const useAdminListParams = (
  options?: Parameters<typeof useQueryStates<typeof adminListParsers>>[1],
) => useQueryStates(adminListParsers, options);

const movieStatuses = [
  "ComingSoon",
  "NowShowing",
  "Ended",
] as const satisfies MovieStatus[];

const adminMovieParsers = {
  page: parseAsInteger.withDefault(DEFAULT_PAGE),
  pageSize: parseAsInteger.withDefault(DEFAULT_PAGE_SIZE),
  search: searchParser,
  status: parseAsStringLiteral(movieStatuses),
  directorId: parseAsInteger,
  languageId: parseAsInteger,
  ageRatingId: parseAsInteger,
  genreId: parseAsInteger,
};

export const useAdminMovieParams = (
  options?: Parameters<typeof useQueryStates<typeof adminMovieParsers>>[1],
) => useQueryStates(adminMovieParsers, options);

const adminShowtimeParsers = {
  page: parseAsInteger.withDefault(DEFAULT_PAGE),
  pageSize: parseAsInteger.withDefault(DEFAULT_PAGE_SIZE),
  movieId: parseAsInteger,
  roomId: parseAsInteger,
  date: parseAsString,
  status: parseAsString,
};

export const useAdminShowtimeParams = (
  options?: Parameters<typeof useQueryStates<typeof adminShowtimeParsers>>[1],
) => useQueryStates(adminShowtimeParsers, options);

const roomClosureScopes = ["active", "ended", "all"] as const;

const adminRoomClosureParsers = {
  page: parseAsInteger.withDefault(DEFAULT_PAGE),
  pageSize: parseAsInteger.withDefault(DEFAULT_PAGE_SIZE),
  roomId: parseAsInteger,
  scope: parseAsStringLiteral(roomClosureScopes).withDefault("active"),
};

export const useAdminRoomClosureParams = (
  options?: Parameters<typeof useQueryStates<typeof adminRoomClosureParsers>>[1],
) => useQueryStates(adminRoomClosureParsers, options);

const adminShowtimeCalendarParsers = {
  from: parseAsString,
  to: parseAsString,
  roomId: parseAsInteger,
  movieId: parseAsInteger,
  status: parseAsString,
};

export const useAdminShowtimeCalendarParams = (
  options?: Parameters<
    typeof useQueryStates<typeof adminShowtimeCalendarParsers>
  >[1],
) => useQueryStates(adminShowtimeCalendarParsers, options);

const adminOrderParsers = {
  page: parseAsInteger.withDefault(DEFAULT_PAGE),
  pageSize: parseAsInteger.withDefault(DEFAULT_PAGE_SIZE),
  search: searchParser,
  status: parseAsString,
  channel: parseAsString,
  type: parseAsString,
  showtimeId: parseAsInteger,
  fromDate: parseAsString,
  toDate: parseAsString,
};

export const useAdminOrderParams = (
  options?: Parameters<typeof useQueryStates<typeof adminOrderParsers>>[1],
) => useQueryStates(adminOrderParsers, options);

const adminPromotionParsers = {
  page: parseAsInteger.withDefault(DEFAULT_PAGE),
  pageSize: parseAsInteger.withDefault(DEFAULT_PAGE_SIZE),
  search: searchParser,
  type: parseAsString,
  isActive: parseAsString,
};

export const useAdminPromotionParams = (
  options?: Parameters<typeof useQueryStates<typeof adminPromotionParsers>>[1],
) => useQueryStates(adminPromotionParsers, options);

const adminBundleParsers = {
  page: parseAsInteger.withDefault(DEFAULT_PAGE),
  pageSize: parseAsInteger.withDefault(DEFAULT_PAGE_SIZE),
  search: searchParser,
  isActive: parseAsString,
};

export const useAdminBundleParams = (
  options?: Parameters<typeof useQueryStates<typeof adminBundleParsers>>[1],
) => useQueryStates(adminBundleParsers, options);

const adminMembershipPlanParsers = {
  page: parseAsInteger.withDefault(DEFAULT_PAGE),
  pageSize: parseAsInteger.withDefault(DEFAULT_PAGE_SIZE),
  search: searchParser,
  isActive: parseAsString,
};

export const useAdminMembershipPlanParams = (
  options?: Parameters<typeof useQueryStates<typeof adminMembershipPlanParsers>>[1],
) => useQueryStates(adminMembershipPlanParsers, options);

const adminCustomerSegmentParsers = {
  page: parseAsInteger.withDefault(DEFAULT_PAGE),
  pageSize: parseAsInteger.withDefault(DEFAULT_PAGE_SIZE),
  search: searchParser,
  isActive: parseAsString,
};

export const useAdminCustomerSegmentParams = (
  options?: Parameters<typeof useQueryStates<typeof adminCustomerSegmentParsers>>[1],
) => useQueryStates(adminCustomerSegmentParsers, options);

const adminCampaignParsers = {
  page: parseAsInteger.withDefault(DEFAULT_PAGE),
  pageSize: parseAsInteger.withDefault(DEFAULT_PAGE_SIZE),
  search: searchParser,
  status: parseAsString,
  segmentId: parseAsString,
};

export const useAdminCampaignParams = (
  options?: Parameters<typeof useQueryStates<typeof adminCampaignParsers>>[1],
) => useQueryStates(adminCampaignParsers, options);

const adminRetentionParsers = {
  months: parseAsInteger.withDefault(12),
};

export const useAdminRetentionParams = (
  options?: Parameters<typeof useQueryStates<typeof adminRetentionParsers>>[1],
) => useQueryStates(adminRetentionParsers, options);

const adminMembershipSubscriptionParsers = {
  page: parseAsInteger.withDefault(DEFAULT_PAGE),
  pageSize: parseAsInteger.withDefault(DEFAULT_PAGE_SIZE),
  search: searchParser,
  status: parseAsString,
  planId: parseAsString,
};

export const useAdminMembershipSubscriptionParams = (
  options?: Parameters<typeof useQueryStates<typeof adminMembershipSubscriptionParsers>>[1],
) => useQueryStates(adminMembershipSubscriptionParsers, options);

const adminLoyaltyRewardParsers = {
  page: parseAsInteger.withDefault(DEFAULT_PAGE),
  pageSize: parseAsInteger.withDefault(DEFAULT_PAGE_SIZE),
  search: searchParser,
  type: parseAsString,
  isActive: parseAsString,
};

export const useAdminLoyaltyRewardParams = (
  options?: Parameters<typeof useQueryStates<typeof adminLoyaltyRewardParsers>>[1],
) => useQueryStates(adminLoyaltyRewardParsers, options);

const adminLoyaltyChallengeParsers = {
  page: parseAsInteger.withDefault(DEFAULT_PAGE),
  pageSize: parseAsInteger.withDefault(DEFAULT_PAGE_SIZE),
  search: searchParser,
  isActive: parseAsString,
};

export const useAdminLoyaltyChallengeParams = (
  options?: Parameters<typeof useQueryStates<typeof adminLoyaltyChallengeParsers>>[1],
) => useQueryStates(adminLoyaltyChallengeParsers, options);

const adminLoyaltyAdjustmentParsers = {
  page: parseAsInteger.withDefault(DEFAULT_PAGE),
  pageSize: parseAsInteger.withDefault(DEFAULT_PAGE_SIZE),
  search: searchParser,
  userId: parseAsInteger,
};

export const useAdminLoyaltyAdjustmentParams = (
  options?: Parameters<typeof useQueryStates<typeof adminLoyaltyAdjustmentParsers>>[1],
) => useQueryStates(adminLoyaltyAdjustmentParsers, options);

const adminPromotionReportParsers = {
  page: parseAsInteger.withDefault(DEFAULT_PAGE),
  pageSize: parseAsInteger.withDefault(DEFAULT_PAGE_SIZE),
  search: searchParser,
  type: parseAsString,
  isActive: parseAsString,
  fromDate: parseAsString,
  toDate: parseAsString,
};

export const useAdminPromotionReportParams = (
  options?: Parameters<typeof useQueryStates<typeof adminPromotionReportParsers>>[1],
) => useQueryStates(adminPromotionReportParsers, options);

const adminPromotionDetailReportParsers = {
  fromDate: parseAsString,
  toDate: parseAsString,
};

export const useAdminPromotionDetailReportParams = (
  options?: Parameters<typeof useQueryStates<typeof adminPromotionDetailReportParsers>>[1],
) => useQueryStates(adminPromotionDetailReportParsers, options);

const adminPromotionRedemptionParsers = {
  page: parseAsInteger.withDefault(DEFAULT_PAGE),
  pageSize: parseAsInteger.withDefault(DEFAULT_PAGE_SIZE),
  search: searchParser,
  status: parseAsString,
  channel: parseAsString,
  fromDate: parseAsString,
  toDate: parseAsString,
};

export const useAdminPromotionRedemptionParams = (
  options?: Parameters<typeof useQueryStates<typeof adminPromotionRedemptionParsers>>[1],
) => useQueryStates(adminPromotionRedemptionParsers, options);

const adminUserParsers = {
  page: parseAsInteger.withDefault(DEFAULT_PAGE),
  pageSize: parseAsInteger.withDefault(DEFAULT_PAGE_SIZE),
  search: searchParser,
  roleId: parseAsInteger,
};

export const useAdminUserParams = (
  options?: Parameters<typeof useQueryStates<typeof adminUserParsers>>[1],
) => useQueryStates(adminUserParsers, options);

const adminEmployeeParsers = {
  page: parseAsInteger.withDefault(DEFAULT_PAGE),
  pageSize: parseAsInteger.withDefault(DEFAULT_PAGE_SIZE),
  positionId: parseAsInteger,
};

export const useAdminEmployeeParams = (
  options?: Parameters<typeof useQueryStates<typeof adminEmployeeParsers>>[1],
) => useQueryStates(adminEmployeeParsers, options);

const adminReviewParsers = {
  page: parseAsInteger.withDefault(DEFAULT_PAGE),
  pageSize: parseAsInteger.withDefault(DEFAULT_PAGE_SIZE),
  movieId: parseAsInteger,
  rating: parseAsInteger,
};

export const useAdminReviewParams = (
  options?: Parameters<typeof useQueryStates<typeof adminReviewParsers>>[1],
) => useQueryStates(adminReviewParsers, options);

const adminCustomerRestrictionParsers = {
  page: parseAsInteger.withDefault(DEFAULT_PAGE),
  pageSize: parseAsInteger.withDefault(DEFAULT_PAGE_SIZE),
  status: parseAsStringLiteral(["Active", "Lifted", "Expired", "All"] as const).withDefault(
    "Active",
  ),
  search: searchParser,
};

export const useAdminCustomerRestrictionParams = (
  options?: Parameters<typeof useQueryStates<typeof adminCustomerRestrictionParsers>>[1],
) => useQueryStates(adminCustomerRestrictionParsers, options);

const adminCashRefundParsers = {
  page: parseAsInteger.withDefault(DEFAULT_PAGE),
  pageSize: parseAsInteger.withDefault(DEFAULT_PAGE_SIZE),
  status: parseAsStringLiteral(["Pending", "Refunded", "All"] as const).withDefault("Pending"),
  orderCode: searchParser,
  fromDate: parseAsString,
  toDate: parseAsString,
};

export const useAdminCashRefundParams = (
  options?: Parameters<typeof useQueryStates<typeof adminCashRefundParsers>>[1],
) => useQueryStates(adminCashRefundParsers, options);

const adminRoomChangeRefundParsers = {
  page: parseAsInteger.withDefault(DEFAULT_PAGE),
  pageSize: parseAsInteger.withDefault(DEFAULT_PAGE_SIZE),
  status: parseAsStringLiteral(["Pending", "Refunded", "Voided", "All"] as const).withDefault(
    "Pending",
  ),
  orderCode: searchParser,
  showtimeId: parseAsInteger,
};

export const useAdminRoomChangeRefundParams = (
  options?: Parameters<typeof useQueryStates<typeof adminRoomChangeRefundParsers>>[1],
) => useQueryStates(adminRoomChangeRefundParsers, options);

const adminInventoryItemParsers = {
  page: parseAsInteger.withDefault(DEFAULT_PAGE),
  pageSize: parseAsInteger.withDefault(DEFAULT_PAGE_SIZE),
  search: searchParser,
  isActive: parseAsString,
  lowStock: parseAsString,
};

export const useAdminInventoryItemParams = (
  options?: Parameters<typeof useQueryStates<typeof adminInventoryItemParsers>>[1],
) => useQueryStates(adminInventoryItemParsers, options);

const adminStockSheetParsers = {
  page: parseAsInteger.withDefault(DEFAULT_PAGE),
  pageSize: parseAsInteger.withDefault(DEFAULT_PAGE_SIZE),
  search: searchParser,
  fromDate: parseAsString,
  toDate: parseAsString,
  itemId: parseAsInteger,
  reason: parseAsString,
};

export const useAdminStockSheetParams = (
  options?: Parameters<typeof useQueryStates<typeof adminStockSheetParsers>>[1],
) => useQueryStates(adminStockSheetParsers, options);

const adminInventoryLedgerParsers = {
  page: parseAsInteger.withDefault(DEFAULT_PAGE),
  pageSize: parseAsInteger.withDefault(DEFAULT_PAGE_SIZE),
  itemId: parseAsInteger,
  fromDate: parseAsString,
  toDate: parseAsString,
};

export const useAdminInventoryLedgerParams = (
  options?: Parameters<typeof useQueryStates<typeof adminInventoryLedgerParsers>>[1],
) => useQueryStates(adminInventoryLedgerParsers, options);

const adminSpecialDayParsers = {
  page: parseAsInteger.withDefault(DEFAULT_PAGE),
  pageSize: parseAsInteger.withDefault(DEFAULT_PAGE_SIZE),
  search: searchParser,
  year: parseAsInteger,
};

export const useAdminSpecialDayParams = (
  options?: Parameters<typeof useQueryStates<typeof adminSpecialDayParsers>>[1],
) => useQueryStates(adminSpecialDayParsers, options);

const adminPricingRuleParsers = {
  page: parseAsInteger.withDefault(DEFAULT_PAGE),
  pageSize: parseAsInteger.withDefault(DEFAULT_PAGE_SIZE),
  search: searchParser,
  ruleType: parseAsString,
};

export const useAdminPricingRuleParams = (
  options?: Parameters<typeof useQueryStates<typeof adminPricingRuleParsers>>[1],
) => useQueryStates(adminPricingRuleParsers, options);

const adminPricePreviewParsers = {
  date: parseAsString,
  showtimeId: parseAsInteger,
  at: parseAsString,
};

export const useAdminPricePreviewParams = (
  options?: Parameters<typeof useQueryStates<typeof adminPricePreviewParsers>>[1],
) => useQueryStates(adminPricePreviewParsers, options);

const adminAuditLogParsers = {
  page: parseAsInteger.withDefault(DEFAULT_PAGE),
  pageSize: parseAsInteger.withDefault(DEFAULT_PAGE_SIZE),
  entityType: parseAsString,
  entityId: searchParser,
  actorUserId: parseAsString,
  fromDate: parseAsString,
  toDate: parseAsString,
};

export const useAdminAuditLogParams = (
  options?: Parameters<typeof useQueryStates<typeof adminAuditLogParsers>>[1],
) => useQueryStates(adminAuditLogParsers, options);

const adminInvoiceRequestParsers = {
  page: parseAsInteger.withDefault(DEFAULT_PAGE),
  pageSize: parseAsInteger.withDefault(DEFAULT_PAGE_SIZE),
  tab: parseAsStringLiteral(["requests", "adjustments"] as const).withDefault("requests"),
  status: parseAsString,
  search: searchParser,
};

export const useAdminInvoiceRequestParams = (
  options?: Parameters<typeof useQueryStates<typeof adminInvoiceRequestParsers>>[1],
) => useQueryStates(adminInvoiceRequestParsers, options);

const adminSupportRequestParsers = {
  page: parseAsInteger.withDefault(DEFAULT_PAGE),
  pageSize: parseAsInteger.withDefault(DEFAULT_PAGE_SIZE),
  status: parseAsStringLiteral(["New", "InProgress", "WaitingCustomer", "Resolved"] as const),
  assignee: parseAsStringLiteral(["all", "mine", "unassigned"] as const).withDefault("all"),
  search: searchParser,
};

export const useAdminSupportRequestParams = (
  options?: Parameters<typeof useQueryStates<typeof adminSupportRequestParsers>>[1],
) => useQueryStates(adminSupportRequestParsers, options);

const adminRefundProposalParsers = {
  page: parseAsInteger.withDefault(DEFAULT_PAGE),
  pageSize: parseAsInteger.withDefault(DEFAULT_PAGE_SIZE),
  status: parseAsStringLiteral(["Pending", "Approved", "Rejected", "All"] as const).withDefault(
    "Pending",
  ),
  orderId: parseAsInteger,
};

export const useAdminRefundProposalParams = (
  options?: Parameters<typeof useQueryStates<typeof adminRefundProposalParsers>>[1],
) => useQueryStates(adminRefundProposalParsers, options);

const adminCounterShiftParsers = {
  page: parseAsInteger.withDefault(DEFAULT_PAGE),
  pageSize: parseAsInteger.withDefault(DEFAULT_PAGE_SIZE),
  employeeId: parseAsString,
  status: parseAsString,
  fromDate: parseAsString,
  toDate: parseAsString,
};

export const useAdminCounterShiftParams = (
  options?: Parameters<typeof useQueryStates<typeof adminCounterShiftParsers>>[1],
) => useQueryStates(adminCounterShiftParsers, options);

const adminDailyClosingHistoryParsers = {
  page: parseAsInteger.withDefault(DEFAULT_PAGE),
  pageSize: parseAsInteger.withDefault(DEFAULT_PAGE_SIZE),
  fromDate: parseAsString,
  toDate: parseAsString,
};

export const useAdminDailyClosingHistoryParams = (
  options?: Parameters<typeof useQueryStates<typeof adminDailyClosingHistoryParsers>>[1],
) => useQueryStates(adminDailyClosingHistoryParsers, options);

const adminDailyClosingReportParsers = {
  mode: parseAsStringLiteral(["OriginalDate", "PostingDate"] as const).withDefault("OriginalDate"),
};

export const useAdminDailyClosingReportParams = (
  options?: Parameters<typeof useQueryStates<typeof adminDailyClosingReportParsers>>[1],
) => useQueryStates(adminDailyClosingReportParsers, options);

const adminLedgerAdjustmentParsers = {
  page: parseAsInteger.withDefault(DEFAULT_PAGE),
  pageSize: parseAsInteger.withDefault(DEFAULT_PAGE_SIZE),
  postingFrom: parseAsString,
  postingTo: parseAsString,
  originalDate: parseAsString,
  type: parseAsString,
};

export const useAdminLedgerAdjustmentParams = (
  options?: Parameters<typeof useQueryStates<typeof adminLedgerAdjustmentParsers>>[1],
) => useQueryStates(adminLedgerAdjustmentParsers, options);

const adminGiftCardParsers = {
  page: parseAsInteger.withDefault(DEFAULT_PAGE),
  pageSize: parseAsInteger.withDefault(DEFAULT_PAGE_SIZE),
  search: searchParser,
  status: parseAsString,
  type: parseAsString,
};

export const useAdminGiftCardParams = (
  options?: Parameters<typeof useQueryStates<typeof adminGiftCardParsers>>[1],
) => useQueryStates(adminGiftCardParsers, options);

const adminDailyClosingParsers = {
  date: parseAsString,
};

export const useAdminDailyClosingParams = (
  options?: Parameters<typeof useQueryStates<typeof adminDailyClosingParsers>>[1],
) => useQueryStates(adminDailyClosingParsers, options);

const adminImportExportJobParsers = {
  page: parseAsInteger.withDefault(DEFAULT_PAGE),
  pageSize: parseAsInteger.withDefault(DEFAULT_PAGE_SIZE),
  type: parseAsString,
  module: parseAsString,
  status: parseAsString,
};

export const useAdminImportExportJobParams = (
  options?: Parameters<
    typeof useQueryStates<typeof adminImportExportJobParsers>
  >[1],
) => useQueryStates(adminImportExportJobParsers, options);

const adminPersonParsers = {
  page: parseAsInteger.withDefault(DEFAULT_PAGE),
  pageSize: parseAsInteger.withDefault(DEFAULT_PAGE_SIZE),
  search: searchParser,
};

export const useAdminPersonParams = (
  options?: Parameters<typeof useQueryStates<typeof adminPersonParsers>>[1],
) => useQueryStates(adminPersonParsers, options);

const clientMovieParsers = {
  cursor: parseAsString,
  limit: parseAsInteger.withDefault(DEFAULT_MOVIES_PAGE_LIMIT),
  status: parseAsString,
  genreId: parseAsInteger,
  languageId: parseAsInteger,
  ageRatingId: parseAsInteger,
  search: searchParser,
};

export const useClientMovieParams = (
  options?: Parameters<typeof useQueryStates<typeof clientMovieParsers>>[1],
) => useQueryStates(clientMovieParsers, options);

const scheduleParsers = {
  date: parseAsString.withDefault(""),
  format: parseAsString,
};

export const useScheduleParams = (
  options?: Parameters<typeof useQueryStates<typeof scheduleParsers>>[1],
) => useQueryStates(scheduleParsers, options);

const occupancyGroups = [
  "Room",
  "TimeSlot",
  "Movie",
  "Weekday",
] as const satisfies OccupancyGroupBy[];

const occupancyKinds = [
  "Regular",
  "Private",
  "Event",
] as const satisfies ShowtimeKind[];

export const DEFAULT_OCCUPANCY_GROUP: OccupancyGroupBy = "Room";
export const DEFAULT_OCCUPANCY_KINDS: ShowtimeKind[] = ["Regular"];

const adminDashboardOccupancyParsers = {
  from: parseAsString,
  to: parseAsString,
  groupBy: parseAsStringLiteral(occupancyGroups).withDefault(DEFAULT_OCCUPANCY_GROUP),
  kind: parseAsArrayOf(parseAsStringLiteral(occupancyKinds)).withDefault(DEFAULT_OCCUPANCY_KINDS),
};

export const useAdminDashboardOccupancyParams = (
  options?: Parameters<typeof useQueryStates<typeof adminDashboardOccupancyParsers>>[1],
) => useQueryStates(adminDashboardOccupancyParsers, options);

const seatHeatmapModes = ["sold", "abandoned"] as const;

export type SeatHeatmapMode = (typeof seatHeatmapModes)[number];

const adminDashboardSeatHeatmapParsers = {
  heatRoom: parseAsInteger,
  heatFrom: parseAsString,
  heatTo: parseAsString,
  heatMovie: parseAsInteger,
  heatGenre: parseAsInteger,
  heatMode: parseAsStringLiteral(seatHeatmapModes).withDefault("sold"),
};

export const useAdminDashboardSeatHeatmapParams = (
  options?: Parameters<typeof useQueryStates<typeof adminDashboardSeatHeatmapParsers>>[1],
) => useQueryStates(adminDashboardSeatHeatmapParsers, options);

const adminScheduleAnalysisParsers = {
  from: parseAsString,
  to: parseAsString,
};

export const useAdminScheduleAnalysisParams = (
  options?: Parameters<typeof useQueryStates<typeof adminScheduleAnalysisParsers>>[1],
) => useQueryStates(adminScheduleAnalysisParsers, options);

export const FINANCE_REPORT_PERIODS = ["day", "week", "month", "custom"] as const;

export type FinanceReportPeriod = (typeof FINANCE_REPORT_PERIODS)[number];

const adminFinanceReportParsers = {
  period: parseAsStringLiteral(FINANCE_REPORT_PERIODS).withDefault("month"),
  date: parseAsString,
  from: parseAsString,
  to: parseAsString,
};

export const useAdminFinanceReportParams = (
  options?: Parameters<typeof useQueryStates<typeof adminFinanceReportParsers>>[1],
) => useQueryStates(adminFinanceReportParsers, options);

const adminReconciliationParsers = {
  page: parseAsInteger.withDefault(DEFAULT_PAGE),
  pageSize: parseAsInteger.withDefault(DEFAULT_PAGE_SIZE),
  gateway: parseAsString,
  fromDate: parseAsString,
  toDate: parseAsString,
  tab: parseAsStringLiteral(["sessions", "items"] as const).withDefault("sessions"),
  result: parseAsString,
  resolution: parseAsString,
  assignee: parseAsInteger,
  sessionId: parseAsInteger,
};

export const useAdminReconciliationParams = (
  options?: Parameters<typeof useQueryStates<typeof adminReconciliationParsers>>[1],
) => useQueryStates(adminReconciliationParsers, options);

const adminRoomRentalParsers = {
  page: parseAsInteger.withDefault(DEFAULT_PAGE),
  pageSize: parseAsInteger.withDefault(DEFAULT_PAGE_SIZE),
  status: parseAsStringLiteral([
    "New",
    "Processing",
    "Quoted",
    "Accepted",
    "Declined",
    "DepositPaid",
    "FullyPaid",
    "Completed",
    "Cancelled",
    "QuoteExpired",
  ] as const),
  view: parseAsStringLiteral(["table", "board"] as const).withDefault("board"),
  search: searchParser,
};

export const useAdminRoomRentalParams = (
  options?: Parameters<typeof useQueryStates<typeof adminRoomRentalParsers>>[1],
) => useQueryStates(adminRoomRentalParsers, options);
