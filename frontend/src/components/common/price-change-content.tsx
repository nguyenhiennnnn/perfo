import { ArrowRight } from "lucide-react";
import { formatCurrency } from "@/lib/formatters";
import type { PriceChangeRequired } from "@/lib/error";

export interface PriceChangeSeatInfo {
  showtimeSeatId: number;
  seatRow: string;
  seatNumber: number;
}

interface PriceChangeContentProps {
  change: PriceChangeRequired;
  seats: PriceChangeSeatInfo[];
  previousTotal: number;
}

export const PriceChangeContent = ({ change, seats, previousTotal }: PriceChangeContentProps) => {
  const seatById = new Map(seats.map((s) => [s.showtimeSeatId, s]));

  return (
    <div className="flex flex-col gap-3 text-sm">
      <ul className="flex flex-col gap-1.5">
        {change.seats.map((item) => {
          const seat = seatById.get(item.showtimeSeatId);
          const label = seat ? `${seat.seatRow}${seat.seatNumber}` : `#${item.showtimeSeatId}`;
          const increased = item.previousPrice != null && item.newPrice > item.previousPrice;
          return (
            <li key={item.showtimeSeatId} className="flex items-center justify-between gap-3">
              <span className="t-code text-foreground">Ghế {label}</span>
              <span className="flex items-center gap-2 tabular-nums">
                {item.previousPrice != null && (
                  <>
                    <span className="text-muted-foreground line-through">
                      {formatCurrency(item.previousPrice)}
                    </span>
                    <ArrowRight className="size-3.5 text-muted-foreground" aria-hidden />
                  </>
                )}
                <span className={increased ? "font-semibold text-destructive" : "font-semibold text-success"}>
                  {formatCurrency(item.newPrice)}
                </span>
              </span>
            </li>
          );
        })}
      </ul>

      <div className="flex items-center justify-between gap-3 border-t border-border pt-3">
        <span className="font-medium text-foreground">Tổng tiền vé</span>
        <span className="flex items-center gap-2 tabular-nums">
          <span className="text-muted-foreground line-through">{formatCurrency(previousTotal)}</span>
          <ArrowRight className="size-3.5 text-muted-foreground" aria-hidden />
          <span className="text-lg font-bold text-primary-text">
            {formatCurrency(change.newTicketTotal)}
          </span>
        </span>
      </div>
    </div>
  );
};
