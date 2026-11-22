import { useRef, type ReactNode } from "react";
import { Ban, TriangleAlert } from "lucide-react";
import { QueryState } from "@/components/common/query-state";
import { CardSkeleton, LineSkeleton } from "@/components/common/skeleton-shapes";
import {
  BookingChannelBadge,
  BookingStatusBadge,
} from "@/components/common/status-badge";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { HTTP_STATUS } from "@/config/http-status";
import { extractApiError, getDisplayMessage } from "@/lib/error";
import { formatCurrency, formatDateTime } from "@/lib/formatters";
import { useAlertDialogOpen } from "@/stores/alert-dialog-store";
import { OrderStatus, type CancelReason } from "@/types/enum";
import {
  useCounterCancelOrderInline,
  useCounterOrderDetail,
} from "../hooks/use-counter-cancel";
import type { CounterOrderDetail } from "../types/counter-cancel";
import {
  canCancelOrder,
  getCancelledByLabel,
  getCancelReasonLabel,
  getCheckInSummary,
  getPaidMethodAmounts,
  getPaymentMethodLabel,
} from "../utils/cancel-order";
import { CounterCancelReasonPicker } from "./counter-cancel-reason-picker";
import { CounterCancelResult } from "./counter-cancel-result";

const DetailSkeleton = () => (
  <div aria-busy="true" className="flex flex-col gap-4 bg-card p-4">
    <CardSkeleton className="h-16" />
    <LineSkeleton className="h-5 w-1/2" />
    <LineSkeleton className="h-5 w-2/3" />
    <CardSkeleton className="h-28" />
    <CardSkeleton className="h-20" />
  </div>
);

const Section = ({ title, children }: { title: string; children: ReactNode }) => (
  <section className="flex flex-col gap-2">
    <h3 className="text-sm font-medium text-muted-foreground">{title}</h3>
    {children}
  </section>
);

const InfoRow = ({ label, children }: { label: string; children: ReactNode }) => (
  <div className="flex items-start justify-between gap-3 text-sm">
    <dt className="shrink-0 text-muted-foreground">{label}</dt>
    <dd className="min-w-0 text-right font-medium wrap-break-word text-foreground">
      {children}
    </dd>
  </div>
);

const getErrorTitle = (error: unknown) => {
  const { statusCode } = extractApiError(error);
  if (statusCode === HTTP_STATUS.conflict)
    return "Đơn đang được xử lý hoặc đã bị hủy";
  if (statusCode === HTTP_STATUS.notFound) return "Không tìm thấy đơn hàng";
  return "Không thể hủy đơn";
};

const CancelErrorNotice = ({ error }: { error: unknown }) => (
  <div
    role="alert"
    data-testid="counter-cancel-error"
    className="flex items-start gap-3 cut-sm bg-destructive/10 p-3 text-destructive"
  >
    <TriangleAlert className="mt-0.5 size-4 shrink-0" />
    <div className="min-w-0">
      <p className="text-sm font-semibold">{getErrorTitle(error)}</p>
      <p className="mt-0.5 text-sm wrap-break-word">
        {getDisplayMessage(error, { preferServerMessage: true })}
      </p>
    </div>
  </div>
);

const OrderLines = ({ order }: { order: CounterOrderDetail }) => (
  <ul className="divide-y divide-border cut-sm bg-secondary px-4">
    {order.lines.map((line) => (
      <li
        key={line.orderLineId}
        className="flex items-start justify-between gap-3 py-3"
      >
        <div className="flex min-w-0 flex-col gap-1">
          {line.ticket ? (
            <>
              <p className="text-sm font-medium text-foreground">
                Vé · {line.ticket.movieTitle}
              </p>
              <p className="text-xs text-muted-foreground">
                Suất chiếu {formatDateTime(line.ticket.startTime)} ·{" "}
                {line.ticket.roomName} · Ghế {line.ticket.seatRow}
                {line.ticket.seatNumber}
              </p>
              <Badge
                variant={line.ticket.isCheckedIn ? "success" : "outline"}
              >
                {line.ticket.isCheckedIn ? "Đã check-in" : "Chưa check-in"}
              </Badge>
            </>
          ) : line.combo ? (
            <>
              <p className="text-sm font-medium text-foreground">
                Combo · {line.combo.name ?? line.description}
              </p>
              <p className="text-xs text-muted-foreground">
                Số lượng {line.quantity} · Đã nhận{" "}
                {line.combo.quantityFulfilled}/{line.quantity}
              </p>
            </>
          ) : (
            <p className="text-sm font-medium text-foreground">
              {line.description ?? line.lineType}
            </p>
          )}
        </div>
        <span className="shrink-0 text-sm font-medium tabular-nums text-foreground">
          {formatCurrency(line.lineTotal)}
        </span>
      </li>
    ))}
  </ul>
);

const PaymentSummary = ({ order }: { order: CounterOrderDetail }) => {
  const methods = getPaidMethodAmounts(order);

  return (
    <dl className="flex flex-col gap-2 cut-sm bg-secondary p-4">
      <InfoRow label="Tổng giá trị đơn">
        <span className="tabular-nums">{formatCurrency(order.finalAmount)}</span>
      </InfoRow>
      <InfoRow label="Khách đã thanh toán">
        <span className="font-semibold tabular-nums">
          {formatCurrency(order.payment.paidAmount)}
        </span>
      </InfoRow>
      {methods.length === 0 ? (
        <InfoRow label="Phương thức thanh toán">Chưa có giao dịch thu</InfoRow>
      ) : (
        <div className="flex flex-col gap-1.5 border-l-2 border-border pl-3">
          {methods.map((item) => (
            <InfoRow key={item.method} label={getPaymentMethodLabel(item.method)}>
              <span className="tabular-nums">{formatCurrency(item.amount)}</span>
            </InfoRow>
          ))}
        </div>
      )}
      {order.payment.refundedAmount > 0 && (
        <InfoRow label="Đã hoàn">
          <span className="tabular-nums">
            {formatCurrency(order.payment.refundedAmount)}
          </span>
        </InfoRow>
      )}
      {order.payment.pendingRefundAmount > 0 && (
        <InfoRow label="Đang chờ hoàn">
          <span className="tabular-nums">
            {formatCurrency(order.payment.pendingRefundAmount)}
          </span>
        </InfoRow>
      )}
    </dl>
  );
};

const ClosedOrderNotice = ({ order }: { order: CounterOrderDetail }) => (
  <div
    data-testid="counter-cancel-closed-notice"
    className="flex flex-col gap-2 cut-sm bg-secondary p-4"
  >
    <p className="text-sm font-medium text-foreground">
      {order.status === OrderStatus.Cancelled
        ? "Đơn hàng này đã được hủy, không thể hủy lại."
        : "Đơn hàng này đã hết hạn giữ vé, không thể hủy."}
    </p>
    {order.status === OrderStatus.Cancelled && (
      <dl className="flex flex-col gap-1.5">
        <InfoRow label="Lý do hủy">
          {order.cancelReason
            ? getCancelReasonLabel(order.cancelReason)
            : "Không ghi nhận"}
        </InfoRow>
        <InfoRow label="Người hủy">
          {order.cancelledBy
            ? getCancelledByLabel(order.cancelledBy)
            : "Không ghi nhận"}
        </InfoRow>
        <InfoRow label="Thời điểm hủy">
          {formatDateTime(order.cancelledAt)}
        </InfoRow>
      </dl>
    )}
  </div>
);

interface CounterCancelOrderDetailProps {
  orderId: number;
}

export const CounterCancelOrderDetail = ({
  orderId,
}: CounterCancelOrderDetailProps) => {
  const detail = useCounterOrderDetail(orderId);
  const cancel = useCounterCancelOrderInline();
  const openAlertDialog = useAlertDialogOpen();
  const reasonRef = useRef<CancelReason | null>(null);

  const handleCancel = (order: CounterOrderDetail) => {
    reasonRef.current = null;
    cancel.reset();
    openAlertDialog({
      title: `Hủy đơn ${order.orderCode}?`,
      description:
        "Thao tác không thể hoàn tác. Số tiền hoàn do hệ thống tính theo chính sách của rạp.",
      content: (
        <CounterCancelReasonPicker
          onReasonChange={(reason) => {
            reasonRef.current = reason;
          }}
        />
      ),
      confirmDisabled: true,
      confirmText: "Xác nhận hủy đơn",
      cancelText: "Giữ lại đơn",
      variant: "destructive",
      onConfirm: async () => {
        const reason = reasonRef.current;
        if (!reason) return;
        try {
          await cancel.mutateAsync({ orderId: order.orderId, reason });
        } catch {
          void detail.refetch();
        }
      },
    });
  };

  return (
    <QueryState
      query={detail}
      skeleton={<DetailSkeleton />}
      errorTitle="Không tải được chi tiết đơn hàng"
    >
      {(order) => {
        const checkIn = getCheckInSummary(order);
        const cancellable = canCancelOrder(order.status);

        return (
          <div
            data-testid="counter-cancel-detail"
            className="flex flex-col gap-5 bg-card p-4"
          >
            <div className="flex flex-wrap items-start justify-between gap-2">
              <div className="min-w-0">
                <p className="t-code font-mono text-lg font-bold tracking-wider text-foreground">
                  {order.orderCode}
                </p>
                <p className="text-xs text-muted-foreground">
                  Tạo lúc {formatDateTime(order.createdAt)}
                </p>
              </div>
              <div className="flex items-center gap-2">
                <BookingChannelBadge channel={order.channel} />
                <BookingStatusBadge status={order.status} />
              </div>
            </div>

            {cancel.data && <CounterCancelResult result={cancel.data.data} />}
            {cancel.error && <CancelErrorNotice error={cancel.error} />}

            <Section title="Khách hàng">
              <dl className="flex flex-col gap-1.5">
                <InfoRow label="Họ tên">
                  {order.customerName ?? "Khách vãng lai"}
                </InfoRow>
                <InfoRow label="Số điện thoại">
                  {order.customerPhone ?? "Chưa có"}
                </InfoRow>
              </dl>
            </Section>

            <Section title="Các dòng hàng">
              <OrderLines order={order} />
            </Section>

            <Section title="Thanh toán">
              <PaymentSummary order={order} />
            </Section>

            {checkIn.total > 0 && (
              <p
                className={
                  checkIn.checkedIn > 0
                    ? "flex items-start gap-2 cut-sm bg-warning/10 px-3 py-2 text-sm text-warning-text"
                    : "text-sm text-muted-foreground"
                }
              >
                {checkIn.checkedIn > 0 ? (
                  <>
                    <TriangleAlert className="mt-0.5 size-4 shrink-0" />
                    <span>
                      Đơn đã có {checkIn.checkedIn}/{checkIn.total} vé check-in,
                      hệ thống sẽ không cho phép hủy đơn này.
                    </span>
                  </>
                ) : (
                  `Chưa có vé nào check-in (0/${checkIn.total}).`
                )}
              </p>
            )}

            {!cancellable && <ClosedOrderNotice order={order} />}

            {cancellable && !cancel.isSuccess && (
              <Button
                variant="destructive"
                className="w-full sm:w-auto sm:self-end"
                disabled={cancel.isPending}
                onClick={() => handleCancel(order)}
              >
                <Ban className="size-4" />
                <span>Hủy đơn</span>
              </Button>
            )}
          </div>
        );
      }}
    </QueryState>
  );
};
