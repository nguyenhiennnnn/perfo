import { useMemo, useState } from "react";
import { AlertTriangle, ArrowRight } from "lucide-react";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { QueryState } from "@/components/common/query-state";
import { SelectableSeatMap } from "@/components/common/seat-map/selectable-seat-map";
import { findLoneSeats, toggleSeatSelection } from "@/components/common/seat-map/seat-map-utils";
import { Button } from "@/components/ui/button";
import { useRealtimeSeatMap } from "@/hooks/use-realtime-seat-map";
import { formatCurrency, formatDateTime } from "@/lib/formatters";
import { SeatGapRuleMode } from "@/types/enum";
import { useCinemaSettings } from "@/features/client/settings/hooks/use-settings";
import { useShowtimeSeats } from "@/features/client/showtimes/hooks/use-showtimes";
import type { ShowtimeSeatItemDto, ShowtimeSeatsResponseDto } from "@/features/client/showtimes/types";
import type { ExchangeChannel } from "../../types";
import type { OrderTicketLine } from "../../utils/order-lines";
import { pairExchangeSeats, seatCode, type ExchangePairing } from "../../utils/order-exchange";
import { ExchangeSummaryCard } from "./exchange-summary-card";

interface ExchangeSeatStepProps {
  channel: ExchangeChannel;
  showtimeId: number;
  lines: OrderTicketLine[];
  isSubmitting: boolean;
  onBack: () => void;
  onConfirm: (pairings: ExchangePairing[], seats: ShowtimeSeatItemDto[]) => void;
}

interface SeatPickerProps extends ExchangeSeatStepProps {
  data: ShowtimeSeatsResponseDto;
}

const SeatPicker = ({ channel, lines, data, isSubmitting, onBack, onConfirm }: SeatPickerProps) => {
  const statusMap = useRealtimeSeatMap({ showtimeId: data.showtimeId, initialSeats: data.seats });
  const gapRuleMode = useCinemaSettings().data?.seatGapRuleMode ?? SeatGapRuleMode.Strict;
  const [selectedIds, setSelectedIds] = useState<Set<number>>(new Set());
  const required = lines.length;

  const toggleSeat = (showtimeSeatId: number) =>
    setSelectedIds((prev) => {
      const next = toggleSeatSelection(prev, data.seats, statusMap, showtimeSeatId);
      return next.size > required ? prev : next;
    });

  const selectedSeats = useMemo(
    () => data.seats.filter((seat) => selectedIds.has(seat.showtimeSeatId)),
    [data.seats, selectedIds],
  );
  const pairings = useMemo(() => pairExchangeSeats(lines, selectedSeats), [lines, selectedSeats]);
  const loneSeats = useMemo(
    () =>
      findLoneSeats(
        data.seats,
        statusMap,
        selectedIds,
        channel === "counter" ? SeatGapRuleMode.Off : gapRuleMode,
      ),
    [channel, data.seats, gapRuleMode, selectedIds, statusMap],
  );

  const isComplete = selectedIds.size === required && pairings.length === required;
  const oldAmount = lines.reduce((sum, line) => sum + line.paidAmount, 0);
  const newAmount = pairings.reduce((sum, pairing) => sum + pairing.newPrice, 0);
  const canConfirm = isComplete && loneSeats.length === 0 && !isSubmitting;

  return (
    <div className="grid grid-cols-1 gap-6 lg:grid-cols-[1fr_22.5rem]">
      <div className="flex flex-col gap-4">
        <section className="cut-md bg-card p-4">
          <h2 className="t-h2 text-foreground">{data.movieTitle}</h2>
          <p className="mt-1 text-sm text-muted-foreground">
            {data.roomName} · {formatDateTime(data.startTime)}
          </p>
          <p className="mt-2 text-sm text-foreground">
            Chọn đúng <span className="font-semibold">{required}</span> ghế để thay cho các vé đang đổi.
          </p>
        </section>

        <SelectableSeatMap
          rows={data.rows}
          cols={data.cols}
          aisles={data.aisles}
          seats={data.seats}
          statusMap={statusMap}
          selectedIds={selectedIds}
          onToggle={(seat) => toggleSeat(seat.showtimeSeatId)}
          fitWidth
        />
      </div>

      <div className="flex flex-col gap-4 lg:sticky lg:top-24 lg:self-start">
        <section className="cut-md bg-card p-4">
          <h3 className="t-h3 text-foreground">Ghế đổi</h3>
          <ul className="mt-3 flex flex-col gap-2 text-sm">
            {lines.map((line) => {
              const pairing = pairings.find((p) => p.line.orderLineId === line.orderLineId);
              return (
                <li key={line.orderLineId} className="flex items-center justify-between gap-2">
                  <span className="t-code text-muted-foreground">
                    {seatCode(line.ticket.seatRow, line.ticket.seatNumber)}
                  </span>
                  <ArrowRight className="size-4 shrink-0 text-muted-foreground" aria-hidden />
                  <span className="flex-1 text-right">
                    {pairing ? (
                      <>
                        <span className="t-code font-medium text-foreground">
                          {seatCode(pairing.seat.seatRow, pairing.seat.seatNumber)}
                        </span>
                        <span className="ml-2 tabular-nums text-muted-foreground">
                          {formatCurrency(pairing.newPrice)}
                        </span>
                      </>
                    ) : (
                      <span className="text-muted-foreground">Chưa chọn</span>
                    )}
                  </span>
                </li>
              );
            })}
          </ul>
        </section>

        {isComplete && (
          <ExchangeSummaryCard
            oldAmount={oldAmount}
            newAmount={newAmount}
            difference={newAmount - oldAmount}
          />
        )}

        {loneSeats.length > 0 && (
          <p className="flex items-start gap-2 bg-warning/10 p-3 text-xs text-foreground">
            <AlertTriangle className="mt-0.5 size-4 shrink-0 text-warning" />
            Không thể để trống ghế lẻ {loneSeats.join(", ")}. Vui lòng chọn lại để không bỏ lẻ ghế.
          </p>
        )}

        <Button disabled={!canConfirm} onClick={() => onConfirm(pairings, selectedSeats)}>
          {isSubmitting ? "Đang giữ ghế..." : "Xác nhận đổi suất"}
        </Button>
        <Button variant="outline" disabled={isSubmitting} onClick={onBack}>
          Chọn suất khác
        </Button>
      </div>
    </div>
  );
};

export const ExchangeSeatStep = (props: ExchangeSeatStepProps) => {
  const seatsQuery = useShowtimeSeats(props.showtimeId);

  return (
    <QueryState
      query={seatsQuery}
      skeleton={
        <div aria-busy="true" className="grid grid-cols-1 gap-6 lg:grid-cols-[1fr_22.5rem]">
          <CardSkeleton className="h-96" cut="lg" />
          <CardSkeleton className="h-48" />
        </div>
      }
      errorTitle="Không tải được sơ đồ ghế"
    >
      {(data) => <SeatPicker {...props} data={data} />}
    </QueryState>
  );
};
