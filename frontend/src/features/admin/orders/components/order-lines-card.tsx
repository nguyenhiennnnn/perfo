import { CheckCircle2 } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { formatCurrency, formatDateTime } from "@/lib/formatters";
import type { OrderLineDto } from "../types";
import { getLineTypeLabel } from "../utils/order-detail";

const Amount = ({ label, value }: { label: string; value: number }) => (
  <div className="flex flex-col gap-0.5">
    <dt className="text-xs text-muted-foreground">{label}</dt>
    <dd className="text-sm font-medium tabular-nums text-foreground">
      {formatCurrency(value)}
    </dd>
  </div>
);

const LineHeading = ({ line }: { line: OrderLineDto }) => {
  if (line.ticket) {
    return (
      <>
        <p className="text-sm font-medium text-foreground">
          Vé · {line.ticket.movieTitle}
        </p>
        <p className="text-xs text-muted-foreground">
          Suất chiếu {formatDateTime(line.ticket.startTime)} · {line.ticket.roomName} ·
          Ghế {line.ticket.seatRow}
          {line.ticket.seatNumber}
        </p>
        {line.ticket.isCheckedIn ? (
          <Badge variant="success" className="w-fit">
            <CheckCircle2 />
            Đã check-in
            {line.ticket.checkedInAt ? ` ${formatDateTime(line.ticket.checkedInAt)}` : ""}
          </Badge>
        ) : (
          <Badge variant="outline" className="w-fit">
            Chưa check-in
          </Badge>
        )}
      </>
    );
  }

  if (line.combo) {
    return (
      <>
        <p className="text-sm font-medium text-foreground">
          Combo · {line.combo.name ?? line.description}
        </p>
        <p className="text-xs text-muted-foreground">
          Số lượng {line.quantity} · Đã nhận {line.combo.quantityFulfilled}/{line.quantity}
        </p>
      </>
    );
  }

  return (
    <>
      <p className="text-sm font-medium text-foreground">
        {getLineTypeLabel(line.lineType)}
      </p>
      {line.description && (
        <p className="text-xs text-muted-foreground">{line.description}</p>
      )}
    </>
  );
};

interface OrderLinesCardProps {
  lines: OrderLineDto[];
}

export const OrderLinesCard = ({ lines }: OrderLinesCardProps) => (
  <section className="cut-md bg-card p-4">
    <h2 className="t-h3 mb-3 text-foreground">Dòng hàng</h2>
    {lines.length === 0 ? (
      <p className="text-sm text-muted-foreground">Đơn hàng chưa có dòng hàng.</p>
    ) : (
      <ul className="divide-y divide-border">
        {lines.map((line) => (
          <li key={line.orderLineId} className="flex flex-col gap-3 py-3 first:pt-0 last:pb-0">
            <div className="flex min-w-0 flex-col gap-1">
              <LineHeading line={line} />
            </div>
            <dl className="grid grid-cols-2 gap-3 sm:grid-cols-5">
              <Amount label="Đơn giá niêm yết" value={line.unitListPrice} />
              <Amount label="Giá đã khóa" value={line.lockedUnitPrice} />
              <Amount label="Thành tiền" value={line.lineTotal} />
              <Amount label="Đã trả" value={line.paidAmount} />
              <div className="flex flex-col gap-0.5">
                <dt className="text-xs text-muted-foreground">Đã hoàn</dt>
                <dd className="text-sm font-medium tabular-nums text-foreground">
                  {formatCurrency(line.refundedAmount)}
                  {line.refundedQuantity > 0 && (
                    <span className="ml-1 text-xs font-normal text-muted-foreground">
                      ({line.refundedQuantity}/{line.quantity})
                    </span>
                  )}
                </dd>
              </div>
            </dl>
          </li>
        ))}
      </ul>
    )}
  </section>
);
