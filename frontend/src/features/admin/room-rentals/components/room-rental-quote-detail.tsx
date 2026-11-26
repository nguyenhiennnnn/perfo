import { QueryState } from "@/components/common/query-state";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { ROOM_RENTAL_QUOTE_LINE_KIND_LABELS, formatRoomRentalCancellationTier, formatRoomRentalDuration } from "@/features/client/room-rentals/utils/room-rental-labels";
import { formatCurrency, formatDateTime } from "@/lib/formatters";
import { useAdminRoomRentalQuote } from "../hooks/use-admin-room-rentals";

export const RoomRentalQuoteDetail = ({
  requestId,
  version,
}: {
  requestId: number;
  version: number;
}) => {
  const quote = useAdminRoomRentalQuote(requestId, version);

  return (
    <QueryState query={quote} skeleton={<CardSkeleton className="h-48" />}>
      {(data) => (
        <div className="space-y-4 text-sm">
          <p className="text-muted-foreground">
            {data.roomName} · {formatDateTime(data.startAt)} · {formatRoomRentalDuration(data.durationMinutes)} ·{" "}
            {data.attendeeCount} người
          </p>
          <ul className="divide-y divide-border">
            {data.lines.map((line, index) => (
              <li key={`${line.kind}-${index}`} className="flex justify-between gap-3 py-1.5">
                <div className="min-w-0">
                  <p className="text-foreground">{line.label}</p>
                  <p className="text-xs text-muted-foreground">
                    {ROOM_RENTAL_QUOTE_LINE_KIND_LABELS[line.kind]}
                    {line.quantity > 1 ? ` · ${line.quantity} × ${formatCurrency(line.unitPrice)}` : ""}
                  </p>
                </div>
                <span className="shrink-0 tabular-nums">{formatCurrency(line.amount)}</span>
              </li>
            ))}
          </ul>
          <dl className="space-y-1 border-t border-border pt-2">
            <div className="flex justify-between">
              <dt className="text-muted-foreground">Tổng cộng</dt>
              <dd className="font-semibold tabular-nums">{formatCurrency(data.total)}</dd>
            </div>
            <div className="flex justify-between">
              <dt className="text-muted-foreground">Đặt cọc ({data.depositPercent}%)</dt>
              <dd className="tabular-nums">{formatCurrency(data.depositAmount)}</dd>
            </div>
            <div className="flex justify-between">
              <dt className="text-muted-foreground">Thanh toán nốt trước {formatDateTime(data.balanceDueAt)}</dt>
              <dd className="tabular-nums">{formatCurrency(data.balanceAmount)}</dd>
            </div>
            <div className="flex justify-between">
              <dt className="text-muted-foreground">Hiệu lực đến</dt>
              <dd className="tabular-nums">{formatDateTime(data.validUntil)}</dd>
            </div>
          </dl>
          {data.cancellationTiers.length > 0 && (
            <div className="space-y-1">
              <p className="font-medium text-foreground">Chính sách hủy</p>
              <ul className="list-disc space-y-0.5 pl-5 text-muted-foreground">
                {data.cancellationTiers.map((tier) => (
                  <li key={tier.minHoursBeforeStart}>
                    {formatRoomRentalCancellationTier(tier.minHoursBeforeStart, tier.refundPercent)}
                  </li>
                ))}
              </ul>
            </div>
          )}
          {data.note && <p className="text-muted-foreground">Ghi chú: {data.note}</p>}
          {data.sentAt && (
            <p className="text-xs text-muted-foreground">
              Đã gửi tới {data.sentToEmail} lúc {formatDateTime(data.sentAt)}
            </p>
          )}
        </div>
      )}
    </QueryState>
  );
};
