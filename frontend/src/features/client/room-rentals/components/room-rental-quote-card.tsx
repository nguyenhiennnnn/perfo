import { Badge } from "@/components/ui/badge";
import { formatCurrency, formatDateTime } from "@/lib/formatters";
import { cn } from "@/lib/utils";
import { RoomRentalQuoteLineKind } from "@/types/enum";
import type { RoomRentalTrackingQuoteDto } from "../types";
import {
  formatRoomRentalCancellationTier,
  formatRoomRentalDuration,
  ROOM_RENTAL_QUOTE_LINE_KIND_LABELS,
} from "../utils/room-rental-labels";
import { RoomRentalPdfButton } from "./room-rental-pdf-button";

interface RoomRentalQuoteCardProps {
  quote: RoomRentalTrackingQuoteDto;
  token: string;
}

interface FactProps {
  label: string;
  value: string;
}

const Fact = ({ label, value }: FactProps) => (
  <div className="flex flex-col gap-0.5">
    <dt className="text-xs text-muted-foreground">{label}</dt>
    <dd className="text-sm font-medium text-foreground">{value}</dd>
  </div>
);

export const RoomRentalQuoteCard = ({ quote, token }: RoomRentalQuoteCardProps) => {
  const tiers = [...quote.cancellationTiers].sort(
    (a, b) => b.minHoursBeforeStart - a.minHoursBeforeStart,
  );

  return (
    <section
      className="cut-md flex flex-col gap-5 bg-card p-4"
      aria-label={`Báo giá phiên bản ${quote.versionNumber}`}
    >
      <div className="flex flex-wrap items-center justify-between gap-2">
        <div className="flex items-center gap-2">
          <h2 className="t-h3 text-foreground">Báo giá phiên bản {quote.versionNumber}</h2>
          <Badge variant={quote.isLatest ? "default" : "secondary"}>
            {quote.isLatest ? "Mới nhất" : "Đã thay thế"}
          </Badge>
        </div>
        <RoomRentalPdfButton token={token} version={quote.versionNumber} />
      </div>

      <dl className="grid grid-cols-2 gap-x-4 gap-y-3">
        <Fact label="Phòng chiếu" value={quote.roomName} />
        <Fact label="Số người" value={`${quote.attendeeCount} người`} />
        <Fact label="Bắt đầu" value={formatDateTime(quote.startAt)} />
        <Fact label="Thời lượng" value={formatRoomRentalDuration(quote.durationMinutes)} />
        <Fact label="Báo giá có hiệu lực đến" value={formatDateTime(quote.validUntil)} />
        <Fact label="Hạn thanh toán phần còn lại" value={formatDateTime(quote.balanceDueAt)} />
      </dl>

      <div className="flex flex-col gap-2">
        <h3 className="text-sm font-semibold text-foreground">Chi tiết báo giá</h3>
        <ul className="flex flex-col divide-y divide-border border-y border-border">
          {quote.lines.map((line, index) => {
            const isDiscount = line.kind === RoomRentalQuoteLineKind.Discount;
            return (
              <li key={`${line.kind}-${index}`} className="flex items-start justify-between gap-3 py-2.5">
                <div className="min-w-0">
                  <p className="text-sm text-foreground">{line.label}</p>
                  <p className="text-xs text-muted-foreground">
                    {ROOM_RENTAL_QUOTE_LINE_KIND_LABELS[line.kind]}
                    {line.quantity > 1 && ` · ${line.quantity} × ${formatCurrency(line.unitPrice)}`}
                  </p>
                </div>
                <span
                  className={cn(
                    "tabular shrink-0 text-sm font-medium",
                    isDiscount ? "text-success" : "text-foreground",
                  )}
                >
                  {isDiscount ? "-" : ""}
                  {formatCurrency(Math.abs(line.amount))}
                </span>
              </li>
            );
          })}
        </ul>

        <dl className="flex flex-col gap-1.5 text-sm">
          <div className="flex justify-between gap-3">
            <dt className="text-muted-foreground">Tạm tính</dt>
            <dd className="tabular text-foreground">{formatCurrency(quote.subtotal)}</dd>
          </div>
          {quote.discountTotal !== 0 && (
            <div className="flex justify-between gap-3">
              <dt className="text-muted-foreground">Chiết khấu</dt>
              <dd className="tabular text-success">-{formatCurrency(Math.abs(quote.discountTotal))}</dd>
            </div>
          )}
          <div className="flex justify-between gap-3 border-t border-border pt-2">
            <dt className="font-semibold text-foreground">Tổng cộng</dt>
            <dd className="tabular text-lg font-bold text-primary-text">{formatCurrency(quote.total)}</dd>
          </div>
        </dl>
      </div>

      <div className="grid gap-3 bg-secondary p-3 sm:grid-cols-2">
        <div>
          <p className="text-xs text-muted-foreground">Đặt cọc ({quote.depositPercent}%)</p>
          <p className="tabular text-base font-semibold text-foreground">
            {formatCurrency(quote.depositAmount)}
          </p>
        </div>
        <div>
          <p className="text-xs text-muted-foreground">Thanh toán phần còn lại</p>
          <p className="tabular text-base font-semibold text-foreground">
            {formatCurrency(quote.balanceAmount)}
          </p>
        </div>
      </div>

      <div className="flex flex-col gap-2">
        <h3 className="text-sm font-semibold text-foreground">Chính sách hủy</h3>
        <ul className="flex list-disc flex-col gap-1 pl-5 text-sm text-foreground">
          {tiers.map((tier) => (
            <li key={tier.minHoursBeforeStart}>
              {formatRoomRentalCancellationTier(tier.minHoursBeforeStart, tier.refundPercent)}
            </li>
          ))}
          <li>Hủy muộn hơn các mốc trên hoặc không thanh toán đúng hạn: không hoàn tiền cọc.</li>
        </ul>
      </div>

      {quote.note && (
        <div className="flex flex-col gap-1">
          <h3 className="text-sm font-semibold text-foreground">Ghi chú từ rạp</h3>
          <p className="whitespace-pre-line text-sm leading-relaxed text-foreground">{quote.note}</p>
        </div>
      )}

      <p className="text-xs text-muted-foreground">Gửi lúc {formatDateTime(quote.sentAt)}</p>
    </section>
  );
};
