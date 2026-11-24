import { formatCurrency } from "@/lib/formatters";
import { cn } from "@/lib/utils";

interface SignedAmountProps {
  value: number;
  count?: boolean;
  className?: string;
}

export const SignedAmount = ({ value, count, className }: SignedAmountProps) => {
  if (value === 0) return <span className={cn("text-muted-foreground", className)}>—</span>;
  const text = count ? String(Math.abs(value)) : formatCurrency(Math.abs(value));
  return (
    <span className={cn("tabular-nums", value > 0 ? "text-success" : "text-destructive", className)}>
      {value > 0 ? "+" : "−"}
      {text}
    </span>
  );
};
