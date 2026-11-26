import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { extractApiError } from "@/lib/error";
import { formatCurrency } from "@/lib/formatters";
import { ROOM_RENTAL_QUOTE_LINE_KIND_LABELS } from "@/features/client/room-rentals/utils/room-rental-labels";
import { RoomRentalQuoteLineKind } from "@/types/enum";
import type { RoomRentalQuoteResultDto } from "../../types";

interface RoomRentalQuotePreviewProps {
  result: RoomRentalQuoteResultDto | undefined;
  isLoading: boolean;
  error: unknown;
  hasInput: boolean;
}

export const RoomRentalQuotePreview = ({
  result,
  isLoading,
  error,
  hasInput,
}: RoomRentalQuotePreviewProps) => (
  <section className="space-y-3 cut-md bg-muted/40 p-3" aria-live="polite" data-testid="quote-preview">
    <h3 className="t-h3 text-foreground">Tạm tính</h3>

    {!hasInput ? (
      <p className="text-sm text-muted-foreground">
        Chọn phòng, ngày giờ, thời lượng và số người để xem tạm tính.
      </p>
    ) : isLoading && !result ? (
      <CardSkeleton className="h-28" />
    ) : error ? (
      <p role="alert" className="text-sm text-destructive">
        {extractApiError(error).message}
      </p>
    ) : result ? (
      <>
        <p className="text-xs text-muted-foreground">
          Bảng giá áp dụng: {result.roomRentalRateName} · {result.roomName}
        </p>
        <ul className="divide-y divide-border">
          {result.lines.map((line, index) => (
            <li key={`${line.kind}-${index}`} className="flex items-start justify-between gap-3 py-1.5 text-sm">
              <div className="min-w-0">
                <p className="text-foreground">{line.label}</p>
                <p className="text-xs text-muted-foreground">
                  {ROOM_RENTAL_QUOTE_LINE_KIND_LABELS[line.kind]}
                  {line.quantity > 1 ? ` · ${line.quantity} × ${formatCurrency(line.unitPrice)}` : ""}
                </p>
              </div>
              <span
                className={
                  line.kind === RoomRentalQuoteLineKind.Discount
                    ? "shrink-0 tabular-nums text-success"
                    : "shrink-0 tabular-nums text-foreground"
                }
              >
                {formatCurrency(line.amount)}
              </span>
            </li>
          ))}
        </ul>
        <dl className="space-y-1 border-t border-border pt-2 text-sm">
          <div className="flex justify-between">
            <dt className="text-muted-foreground">Tạm tính</dt>
            <dd className="tabular-nums">{formatCurrency(result.subtotal)}</dd>
          </div>
          <div className="flex justify-between">
            <dt className="text-muted-foreground">Chiết khấu</dt>
            <dd className="tabular-nums">{formatCurrency(-result.discountTotal)}</dd>
          </div>
          <div className="flex justify-between text-base font-semibold">
            <dt>Tổng cộng</dt>
            <dd className="tabular-nums text-primary-text">{formatCurrency(result.total)}</dd>
          </div>
        </dl>
      </>
    ) : null}
  </section>
);
