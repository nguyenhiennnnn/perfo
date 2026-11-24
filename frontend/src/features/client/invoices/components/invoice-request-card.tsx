import { InvoiceRequestStatusBadge } from "@/components/common/status-badge";
import { DownloadTicketsPdfButton } from "@/components/common/download-tickets-pdf-button";
import { ENDPOINTS } from "@/api/endpoints";
import { formatDateTime } from "@/lib/formatters";
import { InvoiceRequestStatus } from "@/types/enum";
import type { InvoiceRequestDto } from "../types";

interface InvoiceRequestCardProps {
  request: InvoiceRequestDto;
}

export const InvoiceRequestCard = ({ request }: InvoiceRequestCardProps) => (
  <article className="space-y-3 cut-md bg-card p-4" data-testid="invoice-request-card">
    <div className="flex flex-wrap items-start justify-between gap-2">
      <div className="min-w-0">
        <p className="font-medium wrap-break-word text-foreground">{request.companyName}</p>
        <p className="t-code text-sm text-muted-foreground">MST {request.taxCode}</p>
      </div>
      <InvoiceRequestStatusBadge status={request.status} />
    </div>

    <dl className="space-y-1 text-sm">
      <div className="flex gap-2">
        <dt className="shrink-0 text-muted-foreground">Địa chỉ</dt>
        <dd className="min-w-0 wrap-break-word text-foreground">{request.address}</dd>
      </div>
      <div className="flex gap-2">
        <dt className="shrink-0 text-muted-foreground">Email nhận</dt>
        <dd className="min-w-0 break-all text-foreground">{request.email}</dd>
      </div>
      <div className="flex gap-2">
        <dt className="shrink-0 text-muted-foreground">Gửi lúc</dt>
        <dd className="text-foreground">{formatDateTime(request.createdAt)}</dd>
      </div>
      {request.status === InvoiceRequestStatus.Issued && request.invoiceNumber && (
        <div className="flex gap-2">
          <dt className="shrink-0 text-muted-foreground">Số hóa đơn</dt>
          <dd className="t-code font-medium text-foreground">{request.invoiceNumber}</dd>
        </div>
      )}
      {request.processedAt && request.status !== InvoiceRequestStatus.Pending && (
        <div className="flex gap-2">
          <dt className="shrink-0 text-muted-foreground">Xử lý lúc</dt>
          <dd className="text-foreground">{formatDateTime(request.processedAt)}</dd>
        </div>
      )}
    </dl>

    {request.status === InvoiceRequestStatus.Pending && (
      <p className="text-sm text-muted-foreground">
        Yêu cầu đang chờ rạp xuất hóa đơn. Bạn sẽ nhận email khi hóa đơn sẵn sàng.
      </p>
    )}

    {request.status === InvoiceRequestStatus.Rejected && request.rejectReason && (
      <p className="cut-sm bg-destructive/10 px-3 py-2 text-sm text-destructive">
        Lý do từ chối: {request.rejectReason}
      </p>
    )}

    {request.status === InvoiceRequestStatus.Issued && request.needsAdjustment && !request.adjustmentResolvedAt && (
      <p className="cut-sm bg-warning/10 px-3 py-2 text-sm text-warning-text">
        Đơn có phát sinh hoàn tiền sau khi xuất hóa đơn. Rạp sẽ điều chỉnh hóa đơn và thông báo cho bạn.
      </p>
    )}

    {request.status === InvoiceRequestStatus.Issued && request.hasFile && (
      <DownloadTicketsPdfButton
        endpoint={ENDPOINTS.orders.invoiceFile(request.orderId, request.invoiceRequestId)}
        fallbackFilename={`hoa-don-${request.orderCode}.pdf`}
        label="Tải hóa đơn"
        errorTitle="Không thể tải hóa đơn"
      />
    )}
  </article>
);
