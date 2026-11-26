import { Separator } from "@/components/ui/separator";
import { formatCurrency } from "@/lib/formatters";
import type { ComboOption } from "@/components/common/combo-picker/combo-picker";
import type { OrderComboLine, PricedSeat } from "@/features/client/orders/types";
import type { ApplicableBundleDto } from "@/features/client/bundles/types";
import {
  describeBundleItems,
  getBundleSavings,
  getBundleTotal,
} from "@/features/client/bundles/utils/bundle-selection";

export interface BookingBundleSelection {
  bundle: ApplicableBundleDto;
  quantity: number;
}

interface BookingPriceSummaryProps {
  seats: PricedSeat[];
  combos: ComboOption[] | undefined;
  comboQuantities: Record<number, number>;
  bundle?: BookingBundleSelection | null;
}

const groupSeatsByTicketType = (seats: PricedSeat[]) => {
  const groups = new Map<string, { name: string; seats: PricedSeat[]; total: number }>();
  for (const seat of seats) {
    const name = seat.ticketTypeName ?? "Ghế";
    const group = groups.get(name) ?? { name, seats: [], total: 0 };
    group.seats.push(seat);
    group.total += seat.price;
    groups.set(name, group);
  }
  return Array.from(groups.values());
};

export const calcBookingEstimate = (
  seats: PricedSeat[],
  combos: ComboOption[] | undefined,
  comboQuantities: Record<number, number>,
  bundle?: BookingBundleSelection | null,
) => {
  const seatsTotal = seats.reduce((sum, s) => sum + s.price, 0);
  const combosTotal = Object.entries(comboQuantities).reduce((sum, [comboId, qty]) => {
    const combo = combos?.find((c) => c.comboId === Number(comboId));
    return sum + (combo?.price ?? 0) * qty;
  }, 0);
  const bundleTotal = bundle ? getBundleTotal(bundle.bundle, bundle.quantity) : 0;
  return {
    seatsTotal,
    combosTotal,
    bundleTotal,
    total: (bundle ? bundleTotal : seatsTotal) + combosTotal,
  };
};

export const BookingPriceSummary = ({ seats, combos, comboQuantities, bundle }: BookingPriceSummaryProps) => {
  const { total, bundleTotal } = calcBookingEstimate(seats, combos, comboQuantities, bundle);
  const seatGroups = groupSeatsByTicketType(seats);
  const comboLines: OrderComboLine[] = Object.entries(comboQuantities).flatMap(([comboId, quantity]) => {
    const combo = combos?.find((c) => c.comboId === Number(comboId));
    return combo && quantity > 0 ? [{ name: combo.name, quantity, unitPrice: combo.price }] : [];
  });

  return (
    <div className="flex flex-col gap-2 bg-card p-4">
      <h3 className="t-h3 mb-1 text-foreground">Chi tiết đơn hàng</h3>
      {bundle && (
        <div className="flex flex-col gap-0.5">
          <div className="flex items-center justify-between gap-3 text-sm">
            <span className="font-medium text-foreground">
              Gói {bundle.bundle.name}
              {bundle.quantity > 1 ? ` x${bundle.quantity}` : ""}
            </span>
            <span className="font-medium tabular-nums">{formatCurrency(bundleTotal)}</span>
          </div>
          <p className="text-xs text-muted-foreground">{describeBundleItems(bundle.bundle, bundle.quantity)}</p>
          <p className="text-xs tabular-nums text-success">
            Tiết kiệm {formatCurrency(getBundleSavings(bundle.bundle, bundle.quantity))} so với mua lẻ
          </p>
          {seats.length > 0 && (
            <p className="text-xs text-muted-foreground">
              Ghế ({seats.length}): {seats.map((s) => `${s.seatRow}${s.seatNumber}`).join(", ")}
            </p>
          )}
        </div>
      )}
      {!bundle && seatGroups.map((group) => (
        <div key={group.name} className="flex items-center justify-between gap-3 text-sm">
          <span className="text-muted-foreground">
            {group.name} ({group.seats.length}): {group.seats.map((s) => `${s.seatRow}${s.seatNumber}`).join(", ")}
          </span>
          <span className="font-medium tabular-nums">{formatCurrency(group.total)}</span>
        </div>
      ))}
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
