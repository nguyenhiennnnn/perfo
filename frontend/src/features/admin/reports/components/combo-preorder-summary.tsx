import { formatCurrency, formatNumber, formatPercent } from "@/lib/formatters";
import type { FinanceReportBlockDto } from "../types";

export const COMBO_PREORDER_BLOCK_KEY = "combo-preorder";

const toNumber = (value: unknown) => {
  const n = Number(value);
  return value == null || Number.isNaN(n) ? null : n;
};

interface SummaryItemProps {
  label: string;
  value: string;
  hint?: string;
}

const SummaryItem = ({ label, value, hint }: SummaryItemProps) => (
  <div className="flex flex-col gap-1 bg-muted/40 p-3">
    <p className="text-xs text-muted-foreground">{label}</p>
    <p className="text-lg font-bold tabular-nums text-foreground">{value}</p>
    {hint && <p className="text-xs tabular-nums text-muted-foreground">{hint}</p>}
  </div>
);

interface ComboPreorderSummaryProps {
  block: FinanceReportBlockDto;
}

export const ComboPreorderSummary = ({ block }: ComboPreorderSummaryProps) => {
  const totals = block.totals;
  if (!totals) return null;

  const rate = toNumber(totals.supplementRate);
  const preorderRevenue = toNumber(totals.preorderRevenue) ?? 0;
  const counterRevenue = toNumber(totals.counterRevenue) ?? 0;
  const share = toNumber(totals.preorderShare);
  const pickupRate = toNumber(totals.preorderPickupRate);

  return (
    <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 xl:grid-cols-4">
      <SummaryItem
        label="Tỷ lệ khách mua thêm combo"
        value={rate == null ? "—" : formatPercent(rate)}
        hint={`${formatNumber(toNumber(totals.supplementedOrders) ?? 0)}/${formatNumber(toNumber(totals.ticketOrders) ?? 0)} đơn vé`}
      />
      <SummaryItem
        label="Doanh thu combo đặt trước"
        value={formatCurrency(preorderRevenue)}
        hint={`${formatNumber(toNumber(totals.preorderUnits) ?? 0)} combo`}
      />
      <SummaryItem
        label="So với combo mua tại quầy"
        value={share == null ? "—" : formatPercent(share)}
        hint={`Quầy ${formatCurrency(counterRevenue)}`}
      />
      <SummaryItem
        label="Combo đặt trước đã nhận"
        value={pickupRate == null ? "—" : formatPercent(pickupRate)}
        hint={`${formatNumber(toNumber(totals.preorderPickedUp) ?? 0)}/${formatNumber(toNumber(totals.preorderUnits) ?? 0)} combo`}
      />
    </div>
  );
};
