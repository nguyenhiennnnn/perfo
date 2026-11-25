import { CalendarX2, Clock } from "lucide-react";
import { EmptyState } from "@/components/common/empty-state";
import { QueryState } from "@/components/common/query-state";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Label } from "@/components/ui/label";
import { formatDate, formatDateTime, formatTime } from "@/lib/formatters";
import { cn } from "@/lib/utils";
import type {
  OrderExchangePreviewResponse,
  OrderExchangeShowtimePreviewDto,
} from "../../types";
import type { OrderTicketLine } from "../../utils/order-lines";
import { seatCode } from "../../utils/order-exchange";
import { ExchangeDifference } from "./exchange-difference";
import { ExchangeSummaryCard } from "./exchange-summary-card";

interface PreviewQueryLike {
  isLoading: boolean;
  isError: boolean;
  data: OrderExchangePreviewResponse | undefined;
  error?: unknown;
  refetch: () => unknown;
}

interface ExchangeShowtimeStepProps {
  lines: OrderTicketLine[];
  selectedLineIds: number[];
  onToggleLine: (lineId: number, checked: boolean) => void;
  previewQuery: PreviewQueryLike;
  selectedShowtimeId: number | null;
  onSelectShowtime: (showtimeId: number) => void;
  onContinue: () => void;
}

const groupByDate = (showtimes: OrderExchangeShowtimePreviewDto[]) => {
  const groups = new Map<string, OrderExchangeShowtimePreviewDto[]>();
  for (const showtime of showtimes) {
    const key = formatDate(showtime.startTime);
    groups.set(key, [...(groups.get(key) ?? []), showtime]);
  }
  return [...groups.entries()];
};

const ShowtimeOption = ({
  showtime,
  selected,
  onSelect,
}: {
  showtime: OrderExchangeShowtimePreviewDto;
  selected: boolean;
  onSelect: () => void;
}) => (
  <button
    type="button"
    role="radio"
    aria-checked={selected}
    disabled={!showtime.canExchange}
    onClick={onSelect}
    className={cn(
      "flex w-full cursor-pointer flex-col gap-1 cut-sm bg-muted/40 p-3 text-left transition-colors sm:flex-row sm:items-center sm:justify-between sm:gap-4",
      showtime.canExchange ? "hover:bg-muted" : "cursor-not-allowed opacity-60",
      selected && "bg-primary/10 ring-2 ring-primary",
    )}
  >
    <div className="min-w-0">
      <p className="flex items-center gap-2 text-sm font-semibold text-foreground">
        <Clock className="size-4 text-muted-foreground" aria-hidden />
        {formatTime(showtime.startTime)} - {formatTime(showtime.endTime)}
      </p>
      <p className="mt-0.5 text-xs text-muted-foreground">{showtime.roomName}</p>
    </div>
    <div className="text-sm sm:text-right">
      {showtime.canExchange && showtime.pricing ? (
        <ExchangeDifference difference={showtime.pricing.difference} />
      ) : (
        <span className="text-xs text-muted-foreground">{showtime.reason ?? "Không đổi được sang suất này"}</span>
      )}
    </div>
  </button>
);

export const ExchangeShowtimeStep = ({
  lines,
  selectedLineIds,
  onToggleLine,
  previewQuery,
  selectedShowtimeId,
  onSelectShowtime,
  onContinue,
}: ExchangeShowtimeStepProps) => (
  <div className="grid grid-cols-1 gap-6 lg:grid-cols-[1fr_22.5rem]">
    <div className="flex flex-col gap-4">
      <section className="cut-md bg-card p-4">
        <h2 className="t-h2 text-foreground">Vé muốn đổi</h2>
        <p className="mt-1 text-sm text-muted-foreground">
          Chọn các vé cần đổi. Đổi một phần vé hoặc cả đơn đều được.
        </p>
        <ul className="mt-3 flex flex-col gap-2">
          {lines.map((line) => {
            const checkboxId = `exchange-line-${line.orderLineId}`;
            return (
              <li key={line.orderLineId} className="flex items-center gap-3 cut-sm bg-muted/40 p-3">
                <Checkbox
                  id={checkboxId}
                  checked={selectedLineIds.includes(line.orderLineId)}
                  onCheckedChange={(checked) => onToggleLine(line.orderLineId, checked === true)}
                />
                <Label htmlFor={checkboxId} className="flex min-w-0 flex-1 flex-wrap items-center justify-between gap-2">
                  <span className="t-code text-foreground">
                    Ghế {seatCode(line.ticket.seatRow, line.ticket.seatNumber)}
                  </span>
                  <span className="text-xs font-normal text-muted-foreground">
                    {line.ticket.roomName} · {formatDateTime(line.ticket.startTime)}
                  </span>
                </Label>
              </li>
            );
          })}
        </ul>
      </section>

      <section className="cut-md bg-card p-4">
        <h2 className="t-h2 text-foreground">Chọn suất mới</h2>
        <p className="mt-1 text-sm text-muted-foreground">Chỉ hiển thị các suất cùng phim còn mở bán.</p>
        <div className="mt-3">
          {selectedLineIds.length === 0 ? (
            <p className="text-sm text-muted-foreground">Chọn ít nhất một vé để xem các suất có thể đổi.</p>
          ) : (
            <QueryState
              query={previewQuery}
              skeleton={
                <div aria-busy="true" className="flex flex-col gap-2">
                  <CardSkeleton className="h-16" />
                  <CardSkeleton className="h-16" />
                  <CardSkeleton className="h-16" />
                </div>
              }
              isEmpty={(data) => data.showtimes.length === 0}
              empty={
                <EmptyState
                  icon={CalendarX2}
                  title="Chưa có suất nào để đổi"
                  description="Hiện không có suất khác của phim này còn mở bán."
                />
              }
              errorTitle="Không tải được các suất có thể đổi"
            >
              {(data) => (
                <div className="flex flex-col gap-4" role="radiogroup" aria-label="Suất chiếu mới">
                  {groupByDate(data.showtimes).map(([date, showtimes]) => (
                    <div key={date}>
                      <h3 className="mb-2 text-sm font-semibold text-foreground">{date}</h3>
                      <div className="flex flex-col gap-2">
                        {showtimes.map((showtime) => (
                          <ShowtimeOption
                            key={showtime.showtimeId}
                            showtime={showtime}
                            selected={selectedShowtimeId === showtime.showtimeId}
                            onSelect={() => onSelectShowtime(showtime.showtimeId)}
                          />
                        ))}
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </QueryState>
          )}
        </div>
      </section>
    </div>

    <ExchangeShowtimeSidebar
      previewQuery={previewQuery}
      selectedShowtimeId={selectedShowtimeId}
      onContinue={onContinue}
    />
  </div>
);

const ExchangeShowtimeSidebar = ({
  previewQuery,
  selectedShowtimeId,
  onContinue,
}: Pick<ExchangeShowtimeStepProps, "previewQuery" | "selectedShowtimeId" | "onContinue">) => {
  const selected = previewQuery.data?.showtimes.find((s) => s.showtimeId === selectedShowtimeId);
  const pricing = selected?.canExchange ? selected.pricing : null;

  return (
    <div className="flex flex-col gap-4 lg:sticky lg:top-24 lg:self-start">
      {selected && pricing ? (
        <>
          <section className="cut-md bg-card p-4">
            <h3 className="t-h3 text-foreground">Suất mới</h3>
            <p className="mt-2 text-sm font-medium text-foreground">{formatDateTime(selected.startTime)}</p>
            <p className="text-sm text-muted-foreground">{selected.roomName}</p>
          </section>
          <ExchangeSummaryCard
            oldAmount={pricing.oldAmount}
            newAmount={pricing.newAmount}
            difference={pricing.difference}
            title="Chênh lệch dự kiến"
          />
          <p className="text-xs text-muted-foreground">
            Giá chính xác được xác nhận sau khi bạn chọn ghế ở bước tiếp theo.
          </p>
        </>
      ) : (
        <p className="cut-md bg-card p-4 text-sm text-muted-foreground">
          Chọn một suất để xem chênh lệch giá.
        </p>
      )}
      <Button disabled={!pricing} onClick={onContinue}>
        Tiếp tục chọn ghế
      </Button>
    </div>
  );
};
