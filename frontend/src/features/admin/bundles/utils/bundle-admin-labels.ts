import type { EnumOption } from "@/lib/enum-options";
import { getBusinessNow, toWallDate } from "@/lib/business-time";
import { formatDate } from "@/lib/formatters";
import type { AdminBundleDto, AdminBundleItemDto } from "../types";

export const BUNDLE_REPORT_BLOCK_KEY = "bundles";

export const BUNDLE_MAX_PRICE = 1_000_000_000;
export const BUNDLE_MAX_ITEMS = 20;
export const BUNDLE_MAX_ITEM_QUANTITY = 50;
export const BUNDLE_MAX_DAILY_LIMIT = 100_000;

export const ACTIVE_FILTER_OPTIONS: EnumOption[] = [
  { value: "true", label: "Đang bán" },
  { value: "false", label: "Tạm tắt" },
];

export const parseActiveFilter = (value: string | null): boolean | undefined =>
  value === "true" ? true : value === "false" ? false : undefined;

export type BundleItemKind = "ticket" | "combo";

export const BUNDLE_ITEM_KIND_OPTIONS: { value: BundleItemKind; label: string }[] = [
  { value: "ticket", label: "Vé" },
  { value: "combo", label: "Combo" },
];

export type BundleAvailability = "inactive" | "upcoming" | "expired" | "available";

export const deriveBundleAvailability = (
  bundle: AdminBundleDto,
  now: Date = getBusinessNow(),
): BundleAvailability => {
  if (!bundle.isActive) return "inactive";
  const validFrom = toWallDate(bundle.validFrom);
  const validTo = toWallDate(bundle.validTo);
  if (validFrom && validFrom > now) return "upcoming";
  if (validTo && validTo < now) return "expired";
  return "available";
};

export const BUNDLE_AVAILABILITY_BADGES: Record<
  BundleAvailability,
  { label: string; variant: "success" | "secondary" | "warning" }
> = {
  available: { label: "Đang bán", variant: "success" },
  inactive: { label: "Tạm tắt", variant: "secondary" },
  upcoming: { label: "Sắp mở bán", variant: "warning" },
  expired: { label: "Hết hiệu lực", variant: "secondary" },
};

export const describeBundleItem = (item: AdminBundleItemDto) => {
  const name = item.ticketTypeId
    ? (item.ticketTypeName ?? `Loại vé #${item.ticketTypeId}`)
    : (item.comboName ?? `Combo #${item.comboId}`);
  return `${item.quantity} × ${name}`;
};

export const describeBundleValidity = (bundle: AdminBundleDto) => {
  if (!bundle.validFrom && !bundle.validTo) return "Không giới hạn";
  if (bundle.validFrom && bundle.validTo) {
    return `${formatDate(bundle.validFrom)} - ${formatDate(bundle.validTo)}`;
  }
  return bundle.validFrom
    ? `Từ ${formatDate(bundle.validFrom)}`
    : `Đến ${formatDate(bundle.validTo)}`;
};
