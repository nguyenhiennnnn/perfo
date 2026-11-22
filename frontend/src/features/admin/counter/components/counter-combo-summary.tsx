import { ShoppingBasket } from "lucide-react";
import { Separator } from "@/components/ui/separator";
import { formatCurrency } from "@/lib/formatters";
import type { CounterComboLine } from "../hooks/use-counter-combo-sale";

interface CounterComboSummaryProps {
  lines: CounterComboLine[];
  total: number;
}

export const CounterComboSummary = ({ lines, total }: CounterComboSummaryProps) => (
  <div className="flex flex-col gap-2 bg-card p-4">
    {lines.length === 0 ? (
      <div className="flex items-center gap-2 text-sm text-muted-foreground">
        <ShoppingBasket className="size-4" />
        <span>Chưa chọn combo nào.</span>
      </div>
    ) : (
      lines.map((line) => (
        <div
          key={line.comboId}
          className="flex items-start justify-between gap-3 text-sm"
        >
          <span className="min-w-0 text-foreground">
            <span className="wrap-break-word">{line.name}</span>
            <span className="ml-1 text-muted-foreground tabular-nums">
              × {line.quantity}
            </span>
          </span>
          <span className="shrink-0 font-medium tabular-nums">
            {formatCurrency(line.unitPrice * line.quantity)}
          </span>
        </div>
      ))
    )}
    <Separator />
    <div className="flex items-center justify-between">
      <span className="font-medium text-foreground">Tổng cộng</span>
      <span className="text-lg font-bold tabular-nums text-primary-text">
        {formatCurrency(total)}
      </span>
    </div>
  </div>
);
