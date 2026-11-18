import { Separator } from "@/components/ui/separator";
import { formatCurrency } from "@/lib/formatters";
import type { ComboOption } from "@/components/common/combo-picker/combo-picker";
import type { HoldBookingResponse, PricedSeat } from "@/features/client/bookings/types";

export interface BookingComboLine {
  name: string;
  quantity: number;
  unitPrice: number;
}

interface BookingPriceSummaryProps {
  seats: PricedSeat[];
  combos: ComboOption[] | undefined;
  comboQuantities: Record<number, number>;
  comboItems?: BookingComboLine[];
  hold?: HoldBookingResponse | null;
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

export const BookingPriceSummary = ({ seats, combos, comboQuantities, comboItems, hold }: BookingPriceSummaryProps) => {
  const estimate = calcBookingEstimate(seats, combos, comboQuantities);
  const { seatsTotal } = estimate;
  const combosTotal = hold ? Math.max(hold.totalAmount - seatsTotal, 0) : estimate.combosTotal;
  const comboLines: BookingComboLine[] =
    comboItems ??
    Object.entries(comboQuantities).flatMap(([comboId, quantity]) => {
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
      {comboLines.length > 0
        ? comboLines.map((line) => (
            <div key={line.name} className="flex items-center justify-between gap-3 text-sm">
              <span className="text-muted-foreground">
                {line.quantity}x {line.name}
              </span>
              <span className="font-medium tabular-nums">{formatCurrency(line.unitPrice * line.quantity)}</span>
            </div>
          ))
        : combosTotal > 0 && (
            <div className="flex items-center justify-between text-sm">
              <span className="text-muted-foreground">Combo</span>
              <span className="font-medium tabular-nums">{formatCurrency(combosTotal)}</span>
            </div>
          )}
      {hold && hold.discountAmount > 0 && (
        <div className="flex items-center justify-between text-sm">
          <span className="text-muted-foreground">Giảm giá hạng thành viên (áp dụng cho vé)</span>
          <span className="font-medium text-primary-text">-{formatCurrency(hold.discountAmount)}</span>
        </div>
      )}
      <Separator />
      <div className="flex items-center justify-between">
        <span className="font-medium text-foreground">Tổng cộng</span>
        <span className="text-lg font-bold tabular-nums text-primary-text">
          {formatCurrency(hold?.finalAmount ?? estimate.total)}
        </span>
      </div>
    </div>
  );
}