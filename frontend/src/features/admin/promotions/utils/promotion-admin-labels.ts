import { getBusinessNow, toWallDate } from "@/lib/business-time";
import type { EnumOption } from "@/lib/enum-options";
import { saveBlob } from "@/lib/download";
import {
  OrderChannel,
  PromotionDiscountType,
  PromotionRedemptionStatus,
  PromotionScope,
  PromotionType,
} from "@/types/enum";
import type { BadgeVariant } from "@/components/common/status-badge.config";
import type { BulkPersonalCodeItemDto, PromotionDto } from "../types";

export const PROMOTION_CODE_PATTERN = /^[A-Za-z0-9_-]{3,30}$/;
export const PROMOTION_PREFIX_PATTERN = /^[A-Za-z0-9]{2,12}$/;
export const PROMOTION_MAX_PRIORITY = 1000;
export const PROMOTION_MAX_USAGE_LIMIT = 10_000_000;
export const PROMOTION_MAX_AMOUNT = 1_000_000_000;
export const PROMOTION_MAX_BULK_USERS = 500;
export const PROMOTION_MAX_VALID_DAYS = 3650;
export const PROMOTION_NAME_MAX_LENGTH = 100;
export const PROMOTION_DESCRIPTION_MAX_LENGTH = 500;
export const PROMOTION_WARNING_RATIO = 0.8;

export const PROMOTION_TYPE_OPTIONS: (EnumOption<PromotionType> & { hint: string })[] = [
  {
    value: PromotionType.ManualCode,
    label: "Mã nhập tay",
    hint: "Khách nhập mã ở bước thanh toán hoặc quầy nhập giúp khách.",
  },
  {
    value: PromotionType.AutoApply,
    label: "Tự động áp dụng",
    hint: "Hệ thống tự áp dụng cho đơn đủ điều kiện, khách không cần nhập mã.",
  },
  {
    value: PromotionType.PersonalCode,
    label: "Mã cá nhân",
    hint: "Mã dùng một lần, gắn với đúng một tài khoản khách hàng.",
  },
];

export const DISCOUNT_TYPE_OPTIONS: EnumOption<PromotionDiscountType>[] = [
  { value: PromotionDiscountType.Percent, label: "Giảm theo phần trăm" },
  { value: PromotionDiscountType.FixedAmount, label: "Giảm số tiền cố định" },
];

export const SCOPE_OPTIONS: EnumOption<PromotionScope>[] = [
  { value: PromotionScope.TicketsOnly, label: "Chỉ tiền vé" },
  { value: PromotionScope.CombosOnly, label: "Chỉ combo" },
  { value: PromotionScope.WholeOrder, label: "Toàn đơn hàng" },
];

export const ALL_CHANNELS_VALUE = "all";

export const CHANNEL_OPTIONS: EnumOption[] = [
  { value: ALL_CHANNELS_VALUE, label: "Cả online và quầy" },
  { value: OrderChannel.Online, label: "Chỉ online" },
  { value: OrderChannel.Counter, label: "Chỉ tại quầy" },
];

export const CHANNEL_LABELS: Record<string, string> = {
  [OrderChannel.Online]: "Online",
  [OrderChannel.Counter]: "Tại quầy",
};

export const REDEMPTION_STATUS_OPTIONS: EnumOption<PromotionRedemptionStatus>[] = [
  { value: PromotionRedemptionStatus.Reserved, label: "Đang giữ chỗ" },
  { value: PromotionRedemptionStatus.Redeemed, label: "Đã dùng" },
  { value: PromotionRedemptionStatus.Released, label: "Đã nhả lại" },
];

export const REDEMPTION_STATUS_BADGES: Record<
  string,
  { label: string; variant: BadgeVariant }
> = {
  [PromotionRedemptionStatus.Reserved]: { label: "Đang giữ chỗ", variant: "warning" },
  [PromotionRedemptionStatus.Redeemed]: { label: "Đã dùng", variant: "success" },
  [PromotionRedemptionStatus.Released]: { label: "Đã nhả lại", variant: "secondary" },
};

export const ACTIVE_FILTER_OPTIONS: EnumOption[] = [
  { value: "true", label: "Đang bật" },
  { value: "false", label: "Tạm dừng" },
];

export const getPromotionTypeLabel = (type: string) =>
  PROMOTION_TYPE_OPTIONS.find((option) => option.value === type)?.label ?? type;

export const getChannelLabel = (channel?: string | null) =>
  channel ? (CHANNEL_LABELS[channel] ?? channel) : "Cả online và quầy";

export const parseActiveFilter = (value: string | null): boolean | undefined =>
  value === "true" ? true : value === "false" ? false : undefined;

export const TIME_SLOT_INFO: Record<string, { label: string; range: string }> = {
  Morning: { label: "Buổi sáng", range: "06:00 – 12:00" },
  Afternoon: { label: "Buổi chiều", range: "12:00 – 17:00" },
  Evening: { label: "Buổi tối", range: "17:00 – 22:00" },
  Night: { label: "Đêm khuya", range: "22:00 – 06:00" },
};

export const getTimeSlotLabel = (key: string) => {
  const info = TIME_SLOT_INFO[key];
  return info ? `${info.label} (${info.range})` : key;
};

export const ratioToPercent = (ratio?: number | null) =>
  ratio === null || ratio === undefined ? null : Math.round(ratio * 100);

export type UsageLevel = "normal" | "warning" | "exhausted";

export const getUsageLevel = (ratio?: number | null): UsageLevel => {
  if (ratio === null || ratio === undefined) return "normal";
  if (ratio >= 1) return "exhausted";
  if (ratio >= PROMOTION_WARNING_RATIO) return "warning";
  return "normal";
};

export const PromotionStatus = {
  Running: "Running",
  Paused: "Paused",
  Scheduled: "Scheduled",
  Expired: "Expired",
  Exhausted: "Exhausted",
} as const;
export type PromotionStatus = (typeof PromotionStatus)[keyof typeof PromotionStatus];

export const PROMOTION_STATUS_BADGES: Record<
  PromotionStatus,
  { label: string; variant: BadgeVariant }
> = {
  Running: { label: "Đang chạy", variant: "success" },
  Paused: { label: "Tạm dừng", variant: "secondary" },
  Scheduled: { label: "Sắp diễn ra", variant: "outline" },
  Expired: { label: "Hết hạn", variant: "secondary" },
  Exhausted: { label: "Hết lượt / ngân sách", variant: "destructive" },
};

export interface PromotionConsumption {
  consumedCount: number;
  consumedBudget: number;
}

export const isPromotionExhausted = (
  promotion: Pick<PromotionDto, "totalUsageLimit" | "budgetAmount">,
  consumption?: PromotionConsumption | null,
) => {
  if (!consumption) return false;
  const usageDone =
    promotion.totalUsageLimit != null && consumption.consumedCount >= promotion.totalUsageLimit;
  const budgetDone =
    promotion.budgetAmount != null && consumption.consumedBudget >= promotion.budgetAmount;
  return usageDone || budgetDone;
};

export const derivePromotionStatus = (
  promotion: Pick<
    PromotionDto,
    "isActive" | "startsAt" | "endsAt" | "totalUsageLimit" | "budgetAmount"
  >,
  consumption?: PromotionConsumption | null,
): PromotionStatus => {
  if (!promotion.isActive) return PromotionStatus.Paused;
  const now = getBusinessNow().getTime();
  const endsAt = toWallDate(promotion.endsAt)?.getTime();
  if (endsAt !== undefined && endsAt <= now) return PromotionStatus.Expired;
  if (isPromotionExhausted(promotion, consumption)) return PromotionStatus.Exhausted;
  const startsAt = toWallDate(promotion.startsAt)?.getTime();
  if (startsAt !== undefined && startsAt > now) return PromotionStatus.Scheduled;
  return PromotionStatus.Running;
};

const escapeCsv = (value: string) =>
  /[",\r\n]/.test(value) ? `"${value.replace(/"/g, '""')}"` : value;

const CSV_BOM = "\uFEFF";

export const downloadPersonalCodesCsv = (
  items: BulkPersonalCodeItemDto[],
  formatDateTime: (value?: string | null) => string,
  filename: string,
) => {
  const header = ["Mã", "Khách hàng", "Email", "Hạn dùng"];
  const rows = items.map((item) => [
    item.code,
    item.userName,
    item.userEmail,
    item.endsAt ? formatDateTime(item.endsAt) : "Không giới hạn",
  ]);
  const content = [header, ...rows].map((row) => row.map(escapeCsv).join(",")).join("\r\n");
  saveBlob(new Blob([CSV_BOM, content], { type: "text/csv;charset=utf-8" }), filename);
};
