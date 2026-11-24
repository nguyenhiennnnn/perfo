import {
  debounce,
  parseAsInteger,
  parseAsString,
  parseAsStringLiteral,
  useQueryStates,
} from "nuqs";
import type { MovieStatus } from "@/types/enum";
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
