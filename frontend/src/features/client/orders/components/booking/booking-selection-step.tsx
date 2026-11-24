import { useState } from "react";
import { AlertTriangle, PackageOpen } from "lucide-react";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/common/empty-state";
import { SeatTicketList } from "@/components/common/seat-ticket-list";
import { SelectableSeatMap } from "@/components/common/seat-map/selectable-seat-map";
import { formatSeatCodes } from "@/components/common/seat-map/seat-map-utils";
import { ComboPicker } from "@/components/common/combo-picker/combo-picker";
import { useActiveCombos, useComboSuggestions } from "@/features/client/combos/hooks/use-combos";
import { formatCurrency } from "@/lib/formatters";
import { useCinemaSettings } from "@/features/client/settings/hooks/use-settings";
import { ShowtimeSeatStatus } from "@/types/enum";
import type { ShowtimeSeatsResponseDto } from "@/features/client/showtimes/types";
import type { SeatTicketTypes } from "@/features/client/showtimes/hooks/use-seat-ticket-types";

import { BookingCustomerForm } from "./booking-customer-form";
import { BookingPendingOrderNotice } from "./booking-pending-order-notice";
import { BookingPriceSummary, calcBookingEstimate } from "./booking-price-summary";
import { SeatSuggestionPanel } from "./seat-suggestion-panel";
import { BOOKING_COMBO_SECTION_ID, HOLDING_SEATS_LABEL } from "@/config/default-constants";

export const BOOKING_SELECTION_GRID_CLASS = "grid grid-cols-1 gap-6 lg:grid-cols-[1fr_22.5rem]";

interface BookingSelectionStepProps {
  data: ShowtimeSeatsResponseDto | undefined;
  statusMap: Map<number, ShowtimeSeatStatus>;
  selectedIds: Set<number>;
  onToggleSeat: (showtimeSeatId: number) => void;
  onSelectSeats: (showtimeSeatIds: number[]) => void;
  seatTicketTypes: SeatTicketTypes;
  comboQuantities: Record<number, number>;
  onComboChange: (comboId: number, quantity: number) => void;
  customerName: string;
  onCustomerNameChange: (value: string) => void;
  customerPhone: string;
  onCustomerPhoneChange: (value: string) => void;
  isAuthenticated: boolean;
  canHold: boolean;
  loneSeats: string[];
  isHolding: boolean;
  onContinue: () => void;
}

export const BookingSelectionStep = ({
  data,
  statusMap,
  selectedIds,
  onToggleSeat,
  onSelectSeats,
  seatTicketTypes,
  comboQuantities,
  onComboChange,
  customerName,
  onCustomerNameChange,
  customerPhone,
  onCustomerPhoneChange,
  isAuthenticated,
  canHold,
  loneSeats,
  isHolding,
  onContinue,
}: BookingSelectionStepProps) => {
  const activeCombos = useActiveCombos({ alwaysRefetchOnMount: true });
  const holdDurationMinutes = useCinemaSettings().data?.holdDurationMinutes;
  const comboSuggestions = useComboSuggestions(data?.showtimeId ?? 0, selectedIds.size);
  const [highlightedIds, setHighlightedIds] = useState<Set<number>>(new Set());

  if (!data) return null;

  const selectedSeats = seatTicketTypes.pricedSeats.map((s) => ({
    seatRow: s.seatRow,
    seatNumber: s.seatNumber,
    coupleId: s.coupleId,
    price: s.unitPrice,
    ticketTypeName: s.ticketType?.name ?? null,
  }));
  const { total } = calcBookingEstimate(selectedSeats, activeCombos.data, comboQuantities);
  const seatCodes = formatSeatCodes(selectedSeats);
  const suggestions = comboSuggestions.data ?? [];
  const comboRank = new Map(suggestions.map((s, i) => [s.comboId, i]));
  const comboLabels = Object.fromEntries(suggestions.map((s) => [s.comboId, s.label]));
  const allCombosSoldOut = activeCombos.isSuccess && activeCombos.data.length === 0;
  const sortedCombos = activeCombos.data
    ? [...activeCombos.data].sort(
      (a, b) => (comboRank.get(a.comboId) ?? 99) - (comboRank.get(b.comboId) ?? 99),
    )
    : undefined;

  return (
    <div className={BOOKING_SELECTION_GRID_CLASS}>
      <div className="flex flex-col gap-4">
        {data.myHold && <BookingPendingOrderNotice showtimeId={data.showtimeId} hold={data.myHold} />}

        <SeatSuggestionPanel
          showtimeId={data.showtimeId}
          statusMap={statusMap}
          selectedIds={selectedIds}
          onSelect={onSelectSeats}
          onHighlight={(ids) => setHighlightedIds(new Set(ids))}
        />

        <SelectableSeatMap
          rows={data.rows}
          cols={data.cols}
          aisles={data.aisles}
          seats={data.seats}
          statusMap={statusMap}
          selectedIds={selectedIds}
          onToggle={(seat) => onToggleSeat(seat.showtimeSeatId)}
          highlightedIds={highlightedIds}
          fitWidth
        />

        <SeatTicketList
          seats={seatTicketTypes.pricedSeats}
          ticketTypes={seatTicketTypes.ticketTypes}
          onChange={seatTicketTypes.setTicketType}
        />

        <div id={BOOKING_COMBO_SECTION_ID} className="scroll-mt-24 bg-card p-4">
          <h3 className="t-h3 mb-3 text-foreground">Chọn combo (tuỳ chọn)</h3>
          {allCombosSoldOut ? (
            <EmptyState
              icon={PackageOpen}
              title="Combo hiện đã hết hàng"
              description="Bạn vẫn có thể bỏ qua bước này và tiếp tục đặt vé. Combo sẽ hiện lại khi rạp có hàng."
              action={
                <Button variant="outline" disabled={!canHold || isHolding} onClick={onContinue}>
                  Bỏ qua combo & giữ ghế
                </Button>
              }
              className="py-8"
            />
          ) : (
            <ComboPicker
              combos={sortedCombos}
              isLoading={activeCombos.isLoading}
              isError={activeCombos.isError}
              error={activeCombos.error}
              onRetry={() => void activeCombos.refetch()}
              quantities={comboQuantities}
              onChange={onComboChange}
              labels={comboLabels}
            />
          )}
        </div>

        <BookingCustomerForm
          customerName={customerName}
          onCustomerNameChange={onCustomerNameChange}
          customerPhone={customerPhone}
          onCustomerPhoneChange={onCustomerPhoneChange}
          isAuthenticated={isAuthenticated}
        />
      </div>

      <div className="flex flex-col gap-4 lg:sticky lg:top-24 lg:self-start">
        <BookingPriceSummary seats={selectedSeats} combos={activeCombos.data} comboQuantities={comboQuantities} />
        <Button className="max-lg:hidden" disabled={!canHold || isHolding} onClick={onContinue}>
          {isHolding ? HOLDING_SEATS_LABEL : "Giữ ghế & tiếp tục thanh toán"}
        </Button>
        {holdDurationMinutes != null && (
          <p className="text-center text-xs text-muted-foreground">
            Ghế được giữ trong {holdDurationMinutes} phút để bạn hoàn tất thanh toán.
          </p>
        )}
        {loneSeats.length > 0 && (
          <p className="flex items-start gap-2 bg-warning/10 p-3 text-xs text-foreground">
            <AlertTriangle className="mt-0.5 size-4 shrink-0 text-warning" />
            Không thể để trống ghế lẻ {loneSeats.join(", ")}. Vui lòng chọn lại để không bỏ lẻ ghế.
          </p>
        )}
        {selectedSeats.length === 0 && (
          <p className="text-center text-xs text-muted-foreground">Vui lòng chọn ít nhất 1 ghế.</p>
        )}
      </div>

      <div className="sticky bottom-0 z-30 -mx-4 border-t border-border bg-background px-4 pt-3 pb-[calc(0.75rem+env(safe-area-inset-bottom))] lg:hidden">
        {loneSeats.length > 0 && (
          <p className="mb-2 flex items-center gap-1.5 text-xs text-warning-text">
            <AlertTriangle className="size-3.5 shrink-0" />
            <span className="truncate">Không để trống ghế lẻ {loneSeats.join(", ")}</span>
          </p>
        )}
        <div className="flex items-center justify-between gap-3">
          <div className="min-w-0 flex-1">
            <p className="truncate text-xs text-muted-foreground" title={seatCodes.join(", ")}>
              {seatCodes.length > 0 ? (
                <>
                  {seatCodes.length} ghế:{" "}
                  <span className="t-code text-sm text-foreground">{seatCodes.join(", ")}</span>
                </>
              ) : (
                "Chưa chọn ghế"
              )}
            </p>
            <p className="flex items-baseline gap-2">
              <span className="text-lg font-bold tabular-nums text-primary-text">{formatCurrency(total)}</span>
              {holdDurationMinutes != null && seatCodes.length > 0 && (
                <span className="text-xs text-muted-foreground">giữ ghế {holdDurationMinutes} phút</span>
              )}
            </p>
          </div>
          <Button disabled={!canHold || isHolding} onClick={onContinue}>
            {isHolding ? HOLDING_SEATS_LABEL : "Giữ ghế"}
          </Button>
        </div>
      </div>
    </div>
  );
}