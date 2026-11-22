import { useState } from "react";
import { ArrowLeft, ChevronRight, ExternalLink } from "lucide-react";

import { BookingStatusBadge, PaymentMethodBadge } from "@/components/common/status-badge";
import { NotFoundState } from "@/components/common/not-found-state";
import { ButtonLink } from "@/components/common/button-link";
import { QueryState } from "@/components/common/query-state";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { Button } from "@/components/ui/button";
import { ROUTES } from "@/config/routes";
import { formatCurrency, formatDateTime } from "@/lib/formatters";
import { useDialogClose } from "@/stores/dialog-store";
import type { BookingStatus } from "@/types/enum";

import { useAdminPaymentTransactionDetail } from "../hooks/use-payment-ledger";
import type {
  AdminPaymentDetailDto,
  AdminPaymentRelatedTransactionDto,
} from "../types/ledger";
import { isRefundTransaction, needsCashRefund } from "../utils/transaction-labels";
import {
  CashRefundFlagBadge,
  TransactionStatusBadge,
  TransactionTypeBadge,
} from "./transaction-badges";

const Field = ({ label, value }: { label: string; value: string }) => (
  <div className="flex min-w-0 flex-col gap-0.5">
    <dt className="text-xs text-muted-foreground">{label}</dt>
    <dd className="text-sm wrap-break-word text-foreground">{value}</dd>
  </div>
);

const Section = ({ title, children }: { title: string; children: React.ReactNode }) => (
  <section className="space-y-2">
    <h3 className="t-h3 text-foreground">{title}</h3>
    {children}
  </section>
);

interface RelatedRowProps {
  item: AdminPaymentRelatedTransactionDto;
  onOpen: (transactionId: number) => void;
}

const RelatedRow = ({ item, onOpen }: RelatedRowProps) => {
  const isRefund = isRefundTransaction(item.type);

  return (
    <li>
      <button
        type="button"
        onClick={() => onOpen(item.transactionId)}
        className="flex w-full items-center justify-between gap-3 py-2.5 text-left outline-none transition-colors hover:bg-muted/40 focus-visible:bg-muted/40"
      >
        <div className="flex min-w-0 flex-col gap-1.5">
          <div className="flex flex-wrap items-center gap-2">
            <span className="t-code text-foreground">{item.transactionCode}</span>
            <TransactionTypeBadge type={item.type} />
            <TransactionStatusBadge type={item.type} status={item.status} />
            {needsCashRefund(item) && <CashRefundFlagBadge />}
          </div>
          <div className="flex flex-wrap items-center gap-2 text-xs text-muted-foreground">
            <PaymentMethodBadge method={item.method} />
            <span className="tabular-nums">{formatDateTime(item.createdAt)}</span>
          </div>
        </div>
        <div className="flex shrink-0 items-center gap-1">
          <span className="text-sm font-semibold tabular-nums text-foreground">
            {isRefund ? "-" : ""}
            {formatCurrency(item.amount)}
          </span>
          <ChevronRight className="size-4 text-muted-foreground" />
        </div>
      </button>
    </li>
  );
};

interface DetailBodyProps {
  data: AdminPaymentDetailDto;
  onOpenTransaction: (transactionId: number) => void;
}

const DetailBody = ({ data, onOpenTransaction }: DetailBodyProps) => {
  const closeDialog = useDialogClose();
  const isRefund = isRefundTransaction(data.type);
  const summary = data.refundSummary;
  const order = data.order;

  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-center justify-between gap-3 bg-muted/30 p-3">
        <div className="flex flex-wrap items-center gap-2">
          <TransactionTypeBadge type={data.type} />
          <PaymentMethodBadge method={data.method} />
          <TransactionStatusBadge type={data.type} status={data.status} />
          {needsCashRefund(data) && <CashRefundFlagBadge />}
        </div>
        <span className="text-lg font-semibold tabular-nums text-primary-text">
          {isRefund ? "-" : ""}
          {formatCurrency(data.amount)}
        </span>
      </div>

      <dl className="grid grid-cols-1 gap-x-4 gap-y-3 sm:grid-cols-2">
        <Field label="Mã giao dịch hệ thống" value={data.transactionCode} />
        <Field label="Mã giao dịch cổng" value={data.gatewayTransactionId ?? "—"} />
        <Field label="Mã phản hồi cổng" value={data.gatewayResponseCode ?? "—"} />
        <Field label="Phí cổng" value={formatCurrency(data.gatewayFee)} />
        {data.amountReceived != null && (
          <Field label="Tiền nhận" value={formatCurrency(data.amountReceived)} />
        )}
        {data.changeGiven != null && (
          <Field label="Tiền thối" value={formatCurrency(data.changeGiven)} />
        )}
        <Field label="Người thực hiện" value={data.performedByName ?? "Hệ thống"} />
        <Field label="Thời điểm tạo" value={formatDateTime(data.createdAt)} />
        <Field label="Hoàn tất lúc" value={formatDateTime(data.completedAt)} />
        {data.note && (
          <div className="sm:col-span-2">
            <Field label="Ghi chú" value={data.note} />
          </div>
        )}
      </dl>

      {data.originalTransaction && (
        <Section title="Giao dịch thu gốc">
          <ul className="divide-y divide-border border-y border-border">
            <RelatedRow item={data.originalTransaction} onOpen={onOpenTransaction} />
          </ul>
        </Section>
      )}

      {summary && (
        <Section title="Tình trạng hoàn tiền của giao dịch thu">
          <dl className="grid grid-cols-2 gap-x-4 gap-y-3 sm:grid-cols-4">
            <Field label="Đã thu" value={formatCurrency(summary.chargeAmount)} />
            <Field label="Đã hoàn" value={formatCurrency(summary.refundedAmount)} />
            <Field label="Chờ hoàn" value={formatCurrency(summary.pendingRefundAmount)} />
            <Field label="Còn có thể hoàn" value={formatCurrency(summary.refundableAmount)} />
          </dl>
        </Section>
      )}

      {data.relatedRefunds.length > 0 && (
        <Section title={isRefund ? "Các giao dịch hoàn khác của giao dịch gốc" : "Các giao dịch hoàn liên quan"}>
          <ul className="divide-y divide-border border-y border-border">
            {data.relatedRefunds.map((item) => (
              <RelatedRow key={item.transactionId} item={item} onOpen={onOpenTransaction} />
            ))}
          </ul>
        </Section>
      )}

      <Section title="Đơn hàng">
        <div className="flex flex-col gap-3 bg-muted/30 p-3 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex min-w-0 flex-col gap-1">
            <div className="flex flex-wrap items-center gap-2">
              <span className="t-code text-foreground">{order.orderCode}</span>
              <BookingStatusBadge status={order.status as BookingStatus} />
            </div>
            <span className="text-sm text-foreground">
              {order.customerName || "Khách vãng lai"}
              {order.customerPhone ? ` · ${order.customerPhone}` : ""}
            </span>
            {order.movieTitle && (
              <span className="text-xs text-muted-foreground">
                {order.movieTitle}
                {order.startTime ? ` · ${formatDateTime(order.startTime)}` : ""}
              </span>
            )}
            <span className="text-xs tabular-nums text-muted-foreground">
              Tổng đơn: {formatCurrency(order.finalAmount)}
            </span>
          </div>
          <ButtonLink
            to={ROUTES.admin.orderDetail(order.orderId)}
            variant="outline"
            size="sm"
            onClick={closeDialog}
          >
            <ExternalLink />
            Xem chi tiết đơn
          </ButtonLink>
        </div>
      </Section>
    </div>
  );
};

interface PaymentDetailViewProps {
  transactionId: number;
}

export const PaymentDetailView = ({ transactionId }: PaymentDetailViewProps) => {
  const [trail, setTrail] = useState<number[]>([transactionId]);
  const currentId = trail[trail.length - 1];
  const detail = useAdminPaymentTransactionDetail(currentId);

  const openTransaction = (id: number) => setTrail((prev) => [...prev, id]);
  const goBack = () => setTrail((prev) => prev.slice(0, -1));

  return (
    <div className="space-y-4">
      {trail.length > 1 && (
        <Button variant="ghost" size="sm" onClick={goBack}>
          <ArrowLeft />
          Quay lại giao dịch trước
        </Button>
      )}
      <QueryState
        query={detail}
        skeleton={
          <div aria-busy="true" className="space-y-4">
            <CardSkeleton cut="sm" className="h-12" />
            <CardSkeleton className="h-40" />
            <CardSkeleton className="h-24" />
          </div>
        }
        notFound={<NotFoundState entity="giao dịch" />}
        errorTitle="Không tải được chi tiết giao dịch"
        errorClassName="py-8"
      >
        {(data) => <DetailBody data={data} onOpenTransaction={openTransaction} />}
      </QueryState>
    </div>
  );
};
