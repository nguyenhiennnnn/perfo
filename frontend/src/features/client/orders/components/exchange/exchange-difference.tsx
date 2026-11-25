import { cn } from "@/lib/utils";
import { formatCurrency } from "@/lib/formatters";

interface ExchangeDifferenceProps {
  difference: number;
  className?: string;
}

export const ExchangeDifference = ({ difference, className }: ExchangeDifferenceProps) => {
  if (difference > 0) {
    return (
      <span className={cn("font-semibold tabular-nums text-warning-text", className)}>
        Thanh toán thêm {formatCurrency(difference)}
      </span>
    );
  }
  if (difference < 0) {
    return (
      <span className={cn("font-semibold tabular-nums text-success", className)}>
        Nhận tín dụng {formatCurrency(-difference)}
      </span>
    );
  }
  return <span className={cn("font-medium text-muted-foreground", className)}>Không chênh lệch</span>;
};
