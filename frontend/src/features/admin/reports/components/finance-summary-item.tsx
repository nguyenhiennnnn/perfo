interface FinanceSummaryItemProps {
  label: string;
  value: string;
  hint?: string;
}

export const toFinanceNumber = (value: unknown) => {
  const n = Number(value);
  return value == null || Number.isNaN(n) ? null : n;
};

export const FinanceSummaryItem = ({ label, value, hint }: FinanceSummaryItemProps) => (
  <div className="flex flex-col gap-1 bg-muted/40 p-3">
    <p className="text-xs text-muted-foreground">{label}</p>
    <p className="text-lg font-bold tabular-nums text-foreground">{value}</p>
    {hint && <p className="text-xs tabular-nums text-muted-foreground">{hint}</p>}
  </div>
);
