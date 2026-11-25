import type { ReactNode } from "react";
import { Link } from "react-router";
import { SupportRequestStatusBadge } from "@/components/common/status-badge";
import { ROUTES } from "@/config/routes";
import { formatDateTime } from "@/lib/formatters";
import { SupportRequestStatus } from "@/types/enum";
import type { SupportRequestDto } from "../types";
import { getSupportTypeLabel } from "../utils/support-labels";

interface SupportRequestHeaderProps {
  request: SupportRequestDto;
}

const Row = ({ label, children }: { label: string; children: ReactNode }) => (
  <div className="flex gap-2">
    <dt className="shrink-0 text-muted-foreground">{label}</dt>
    <dd className="min-w-0 wrap-break-word text-foreground">{children}</dd>
  </div>
);

export const SupportRequestHeader = ({ request }: SupportRequestHeaderProps) => {
  const isResolved = request.status === SupportRequestStatus.Resolved;

  return (
    <div className="space-y-3 cut-md bg-card p-4" data-testid="support-request-header">
      <div className="flex flex-wrap items-start justify-between gap-2">
        <div className="min-w-0">
          <p className="text-xs text-muted-foreground">{getSupportTypeLabel(request.type)}</p>
          <h1 className="t-h1 wrap-break-word text-foreground">{request.subject}</h1>
        </div>
        <span data-testid="support-request-status">
          <SupportRequestStatusBadge status={request.status} />
        </span>
      </div>

      <dl className="space-y-1 text-sm">
        <Row label="Đơn hàng">
          <Link
            to={ROUTES.account.ticketDetail(request.orderId)}
            className="t-code text-primary-text underline-offset-4 hover:underline"
          >
            {request.orderCode}
          </Link>
        </Row>
        <Row label="Gửi lúc">{formatDateTime(request.createdAt)}</Row>
        <Row label="Người phụ trách">{request.assigneeName ?? "Chưa phân công"}</Row>
        {isResolved ? (
          request.resolvedAt && <Row label="Giải quyết lúc">{formatDateTime(request.resolvedAt)}</Row>
        ) : (
          <Row label="Dự kiến phản hồi trước">{formatDateTime(request.dueAt)}</Row>
        )}
      </dl>

      {request.status === SupportRequestStatus.WaitingCustomer && (
        <p className="cut-sm bg-primary/10 px-3 py-2 text-sm text-foreground">
          Rạp đang chờ phản hồi của bạn. Hãy trả lời bên dưới để tiếp tục xử lý.
        </p>
      )}
    </div>
  );
};
