import { Link } from "react-router";
import { ChevronRight } from "lucide-react";
import { SupportRequestStatusBadge } from "@/components/common/status-badge";
import { ROUTES } from "@/config/routes";
import { formatDateTime } from "@/lib/formatters";
import { SupportRequestStatus } from "@/types/enum";
import type { SupportRequestListItemDto } from "../types";
import { getSupportTypeLabel } from "../utils/support-labels";

interface SupportRequestCardProps {
  request: SupportRequestListItemDto;
}

export const SupportRequestCard = ({ request }: SupportRequestCardProps) => {
  const isResolved = request.status === SupportRequestStatus.Resolved;

  return (
    <Link
      to={ROUTES.account.supportRequestDetail(request.supportRequestId)}
      aria-label={`Yêu cầu hỗ trợ: ${request.subject}`}
      data-testid="support-request-card"
      className="flex items-center justify-between gap-4 cut-md bg-card p-4 transition-colors hover:bg-muted/50"
    >
      <div className="min-w-0 space-y-1">
        <div className="flex flex-wrap items-center gap-2">
          <SupportRequestStatusBadge status={request.status} />
          <span className="text-xs text-muted-foreground">{getSupportTypeLabel(request.type)}</span>
        </div>
        <h2 className="line-clamp-2 font-heading text-sm font-semibold wrap-break-word text-foreground">
          {request.subject}
        </h2>
        <p className="text-xs text-muted-foreground">
          Đơn <span className="t-code">{request.orderCode}</span>
        </p>
        <p className="text-xs text-muted-foreground">
          Cập nhật {formatDateTime(request.lastMessageAt)}
          {!isResolved && `, dự kiến phản hồi trước ${formatDateTime(request.dueAt)}`}
        </p>
      </div>
      <ChevronRight className="size-5 shrink-0 text-muted-foreground" aria-hidden />
    </Link>
  );
};
