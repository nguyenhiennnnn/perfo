import { formatNumber } from "@/lib/formatters";
import type { BadgeVariant } from "@/components/common/status-badge.config";
import { StockMovementType, WasteReason } from "../types";

export const INVENTORY_MAX_QUANTITY = 1_000_000;
export const INVENTORY_MAX_UNIT_COST = 1_000_000_000;
export const INVENTORY_QUANTITY_DECIMALS = 3;
export const INVENTORY_QUANTITY_STEP = 0.001;
export const INVENTORY_CODE_PATTERN = /^[A-Za-z0-9_-]{2,30}$/;
export const INVENTORY_CODE_MAX_LENGTH = 30;
export const INVENTORY_NAME_MAX_LENGTH = 150;
export const INVENTORY_UNIT_MAX_LENGTH = 20;
export const SUPPLIER_NAME_MAX_LENGTH = 150;
export const STOCK_NOTE_MAX_LENGTH = 255;
export const RECIPE_MAX_LINES = 30;
export const RECEIPT_MAX_LINES = 100;
export const STOCKTAKE_MAX_LINES = 200;
export const WASTE_MAX_LINES = 50;
export const LEDGER_PAGE_SIZE = 20;

const QUANTITY_FORMAT: Intl.NumberFormatOptions = {
  maximumFractionDigits: INVENTORY_QUANTITY_DECIMALS,
};

export const formatQuantity = (value?: number | null, unit?: string | null) => {
  const text = formatNumber(value, QUANTITY_FORMAT);
  return unit ? `${text} ${unit}` : text;
};

export const formatSignedQuantity = (value: number, unit?: string | null) => {
  if (value === 0) return formatQuantity(0, unit);
  const sign = value > 0 ? "+" : "−";
  return `${sign}${formatQuantity(Math.abs(value), unit)}`;
};

export const getSignedTextClass = (value: number) =>
  value > 0 ? "text-success" : value < 0 ? "text-destructive" : "text-muted-foreground";

export const MOVEMENT_LABELS: Record<string, string> = {
  [StockMovementType.Receipt]: "Nhập kho",
  [StockMovementType.Sale]: "Bán hàng",
  [StockMovementType.Return]: "Hoàn kho",
  [StockMovementType.StocktakeAdjustment]: "Điều chỉnh kiểm kê",
  [StockMovementType.Waste]: "Hao hụt",
};

export const MOVEMENT_VARIANTS: Record<string, BadgeVariant> = {
  [StockMovementType.Receipt]: "success",
  [StockMovementType.Sale]: "secondary",
  [StockMovementType.Return]: "outline",
  [StockMovementType.StocktakeAdjustment]: "warning",
  [StockMovementType.Waste]: "destructive",
};

export const getMovementLabel = (type: string) => MOVEMENT_LABELS[type] ?? type;
export const getMovementVariant = (type: string): BadgeVariant => MOVEMENT_VARIANTS[type] ?? "outline";

export const WASTE_REASON_LABELS: Record<string, string> = {
  [WasteReason.Expired]: "Hết hạn",
  [WasteReason.Damaged]: "Hư hỏng",
  [WasteReason.Spilled]: "Đổ vỡ",
  [WasteReason.Other]: "Lý do khác",
};

export const WASTE_REASON_OPTIONS = Object.values(WasteReason).map((value) => ({
  value,
  label: WASTE_REASON_LABELS[value],
}));

export const getWasteReasonLabel = (reason: string) => WASTE_REASON_LABELS[reason] ?? reason;

const REFERENCE_LABELS: Record<string, string> = {
  StockReceipt: "Phiếu nhập",
  Stocktake: "Phiếu kiểm kê",
  Waste: "Phiếu hao hụt",
  OrderLine: "Dòng đơn hàng",
};

export const formatLedgerReference = (type?: string | null, id?: number | null) => {
  if (!type) return null;
  const label = REFERENCE_LABELS[type] ?? type;
  return id == null ? label : `${label} #${id}`;
};

export const formatLedgerReason = (movementType: string, reason?: string | null) => {
  if (!reason) return null;
  if (movementType !== StockMovementType.Waste) return reason;
  const [code, ...rest] = reason.split(": ");
  const label = WASTE_REASON_LABELS[code];
  if (!label) return reason;
  return rest.length > 0 ? `${label}: ${rest.join(": ")}` : label;
};

export const QUANTITY_EPSILON = 0.0005;
