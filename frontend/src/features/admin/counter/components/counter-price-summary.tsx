import { Separator } from "@/components/ui/separator";
import { formatCurrency } from "@/lib/formatters";
import type { ShowtimeSeatItemDto } from "@/features/client/showtimes/types";
import type { ComboOption } from "@/components/common/combo-picker/combo-picker";

interface CounterPriceSummaryProps {
  seats: ShowtimeSeatItemDto[];
  combos: ComboOption[] | undefined;
  comboQuantities: Record<number, number>;
  discountAmount?: number;
  finalAmount?: number;
}

export const CounterPriceSummary = ({
  seats,
  combos,
  comboQuantities,
  discountAmount = 0,
  finalAmount,
}: CounterPriceSummaryProps) => {
  const seatsTotal = seats.reduce((sum, s) => sum + s.price, 0);
  const combosTotal = Object.entries(comboQuantities).reduce((sum, [comboId, qty]) => {
    const combo = combos?.find((c) => c.comboId === Number(comboId));
    return sum + (combo?.price ?? 0) * qty;
  }, 0);
  const estimatedTotal = seatsTotal + combosTotal;
  const isEmpty = seats.length === 0 && combosTotal === 0;

  return (
    <div className="flex flex-col gap-2 bg-card p-4">
      {isEmpty ? (
        <p className="text-sm text-muted-foreground">Chưa chọn ghế hoặc combo nào.</p>
      ) : (
        <>
          <div className="flex items-center justify-between text-sm">
            <span className="text-muted-foreground">Ghế ({seats.length})</span>
            <span className="font-medium tabular-nums">{formatCurrency(seatsTotal)}</span>
          </div>
          {combosTotal > 0 && (
            <div className="flex items-center justify-between text-sm">
              <span className="text-muted-foreground">Combo</span>
              <span className="font-medium tabular-nums">{formatCurrency(combosTotal)}</span>
            </div>
          )}
          {discountAmount > 0 && (
            <div className="flex items-center justify-between text-sm">
              <span className="text-muted-foreground">Giảm giá</span>
              <span className="font-medium tabular-nums text-success">
                -{formatCurrency(discountAmount)}
              </span>
            </div>
          )}
        </>
      )}
      <Separator />
      <div className="flex items-center justify-between">
        <span className="font-medium text-foreground">Tổng cộng</span>
        <span className="text-lg font-bold tabular-nums text-primary-text">
          {formatCurrency(finalAmount ?? estimatedTotal)}
        </span>
      </div>
    </div>
  );
};
