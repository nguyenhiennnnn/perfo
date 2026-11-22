import { Separator } from "@/components/ui/separator";
import { formatCurrency } from "@/lib/formatters";
import type { ComboOption } from "@/components/common/combo-picker/combo-picker";
import type { OrderComboLine, PricedSeat } from "@/features/client/orders/types";

interface BookingPriceSummaryProps {
  seats: PricedSeat[];
  combos: ComboOption[] | undefined;
  comboQuantities: Record<number, number>;
}

export const calcBookingEstimate = (
  seats: PricedSeat[],
  combos: ComboOption[] | undefined,
  comboQuantities: Record<number, number>,
) => {
  const seatsTotal = seats.reduce((sum, s) => sum + s.price, 0);
  const combosTotal = Object.entries(comboQuantities).reduce((sum, [comboId, qty]) => {
    const combo = combos?.find((c) => c.comboId === Number(comboId));
    return sum + (combo?.price ?? 0) * qty;
  }, 0);
  return { seatsTotal, combosTotal, total: seatsTotal + combosTotal };
};

export const BookingPriceSummary = ({ seats, combos, comboQuantities }: BookingPriceSummaryProps) => {
  const { seatsTotal, total } = calcBookingEstimate(seats, combos, comboQuantities);
  const comboLines: OrderComboLine[] = Object.entries(comboQuantities).flatMap(([comboId, quantity]) => {
    const combo = combos?.find((c) => c.comboId === Number(comboId));
    return combo && quantity > 0 ? [{ name: combo.name, quantity, unitPrice: combo.price }] : [];
  });

  return (
    <div className="flex flex-col gap-2 bg-card p-4">
      <h3 className="t-h3 mb-1 text-foreground">Chi tiết đơn hàng</h3>
      <div className="flex items-center justify-between text-sm">
        <span className="text-muted-foreground">
          Ghế ({seats.length}): {seats.map((s) => `${s.seatRow}${s.seatNumber}`).join(", ")}
        </span>
        <span className="font-medium">{formatCurrency(seatsTotal)}</span>
      </div>
      {comboLines.map((line) => (
        <div key={line.name} className="flex items-center justify-between gap-3 text-sm">
          <span className="text-muted-foreground">
            {line.quantity}x {line.name}
          </span>
          <span className="font-medium tabular-nums">{formatCurrency(line.unitPrice * line.quantity)}</span>
        </div>
      ))}
      <Separator />
      <div className="flex items-center justify-between">
        <span className="font-medium text-foreground">Tổng cộng</span>
        <span className="text-lg font-bold tabular-nums text-primary-text">{formatCurrency(total)}</span>
      </div>
    </div>
  );
};
