import type { EnumOption } from "@/lib/enum-options";

export const LEDGER_ADJUSTMENT_TYPE_LABELS: Record<string, string> = {
  Refund: "Hoàn tiền muộn",
  Cancellation: "Hủy đơn muộn",
  ShowtimeChange: "Đổi suất",
  Manual: "Thủ công",
};

export const getLedgerAdjustmentTypeLabel = (type: string) =>
  LEDGER_ADJUSTMENT_TYPE_LABELS[type] ?? type;

export const ledgerAdjustmentTypeOptions: EnumOption[] = Object.entries(
  LEDGER_ADJUSTMENT_TYPE_LABELS,
).map(([value, label]) => ({ value, label }));
