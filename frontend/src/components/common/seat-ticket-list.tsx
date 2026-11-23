import { IdCard, Info } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { formatCurrency } from "@/lib/formatters";
import type { ShowtimeTicketTypeDto } from "@/features/client/showtimes/types";
import {
  getSeatPrice,
  type PricedSelectedSeat,
} from "@/features/client/showtimes/utils/seat-pricing";

type SeatTicketListAudience = "customer" | "counter";

interface SeatTicketListProps {
  seats: PricedSelectedSeat[];
  ticketTypes: ShowtimeTicketTypeDto[];
  onChange: (showtimeSeatId: number, ticketTypeId: number | null) => void;
  audience?: SeatTicketListAudience;
  disabled?: boolean;
}

const CARD_NOTE: Record<SeatTicketListAudience, string> = {
  customer: "Vui lòng mang theo thẻ phù hợp. Nhân viên sẽ kiểm tra khi bạn vào rạp.",
  counter: "Nhắc khách xuất trình thẻ ngay tại quầy và khi vào rạp.",
};

export const SeatTicketList = ({
  seats,
  ticketTypes,
  onChange,
  audience = "customer",
  disabled,
}: SeatTicketListProps) => {
  if (seats.length === 0 || ticketTypes.length === 0) return null;

  const cardSeats = seats.filter((s) => s.ticketType?.requiresCardVerification);
  const restricted = ticketTypes.filter((t) => t.requiresCardVerification || !t.isEligible);

  return (
    <div className="flex flex-col gap-3 bg-card p-4">
      <h3 className="t-h3 text-foreground">Loại vé từng ghế</h3>

      <ul className="flex flex-col gap-2">
        {seats.map((seat) => {
          const code = `${seat.seatRow}${seat.seatNumber}`;
          return (
            <li
              key={seat.showtimeSeatId}
              className="grid grid-cols-[3.5rem_1fr_auto] items-center gap-3"
            >
              <span className="t-code text-sm text-foreground">{code}</span>
              <Select
                value={seat.ticketTypeId != null ? String(seat.ticketTypeId) : undefined}
                onValueChange={(v) => onChange(seat.showtimeSeatId, v ? Number(v) : null)}
                disabled={disabled}
              >
                <SelectTrigger size="sm" className="w-full" aria-label={`Loại vé ghế ${code}`}>
                  <SelectValue placeholder="Chọn loại vé">
                    {seat.ticketType?.name ?? "Chọn loại vé"}
                  </SelectValue>
                </SelectTrigger>
                <SelectContent>
                  {ticketTypes.map((type) => (
                    <SelectItem
                      key={type.ticketTypeId}
                      value={String(type.ticketTypeId)}
                      disabled={!type.isEligible}
                    >
                      {type.name}
                      {type.requiresCardVerification ? " (cần thẻ)" : ""}
                      {type.isEligible
                        ? `, ${formatCurrency(getSeatPrice(seat, type.ticketTypeId))}`
                        : ", không áp dụng"}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <span className="min-w-20 text-right text-sm font-medium tabular-nums">
                {formatCurrency(seat.unitPrice)}
              </span>
            </li>
          );
        })}
      </ul>

      {cardSeats.length > 0 && (
        <p
          role="note"
          className="flex items-start gap-2 bg-warning/10 p-3 text-xs text-foreground"
        >
          <IdCard className="mt-0.5 size-4 shrink-0 text-warning" />
          <span>
            Ghế {cardSeats.map((s) => `${s.seatRow}${s.seatNumber}`).join(", ")} cần xuất trình
            thẻ. {CARD_NOTE[audience]}
          </span>
        </p>
      )}

      {restricted.length > 0 && (
        <div className="flex flex-col gap-1.5 border-t border-border pt-3">
          <p className="flex items-center gap-1.5 text-xs font-medium text-muted-foreground">
            <Info className="size-3.5" />
            Điều kiện loại vé
          </p>
          <ul className="flex flex-col gap-1 text-xs text-muted-foreground">
            {restricted.map((type) => (
              <li key={type.ticketTypeId} className="flex flex-wrap items-center gap-1.5">
                <span className="font-medium text-foreground">{type.name}:</span>
                {type.requiresCardVerification && <Badge variant="warning">Cần xuất trình thẻ</Badge>}
                {!type.isEligible && (
                  <span>{type.ineligibleReason ?? "Không áp dụng cho suất này"}</span>
                )}
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );
};
