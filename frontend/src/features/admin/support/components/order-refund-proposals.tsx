import { Link } from "react-router";
import { ExceptionalRefundStatusBadge } from "@/components/common/status-badge";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { ROUTES } from "@/config/routes";
import { formatCurrency, formatDateTime } from "@/lib/formatters";
import { useAuthRole, useAuthUser } from "@/stores/auth-store";
import { ExceptionalRefundStatus, Role } from "@/types/enum";
import { useRefundProposalActions } from "../hooks/use-refund-proposal-actions";
import { useRefundProposals } from "../hooks/use-refund-proposals";

const PROPOSALS_PAGE_SIZE = 20;

export const OrderRefundProposals = ({ orderId }: { orderId: number }) => {
  const proposals = useRefundProposals({ orderId, pageSize: PROPOSALS_PAGE_SIZE });
  const user = useAuthUser();
  const isAdmin = useAuthRole() === Role.Admin;
  const actions = useRefundProposalActions();

  if (proposals.isLoading) return <Skeleton className="h-16 w-full" />;

  const items = proposals.data?.items ?? [];
  if (items.length === 0)
    return <p className="text-sm text-muted-foreground">Chưa có đề xuất hoàn tiền ngoại lệ cho đơn này.</p>;

  return (
    <ul className="space-y-3">
      {items.map((proposal) => {
        const canReview = isAdmin && proposal.status === ExceptionalRefundStatus.Pending;
        const isOwn = proposal.proposedByUserId === user?.userId;
        return (
          <li key={proposal.exceptionalRefundProposalId} className="space-y-2 cut-sm bg-card p-3">
            <div className="flex flex-wrap items-center justify-between gap-2">
              <span className="font-semibold tabular-nums text-primary-text">{formatCurrency(proposal.amount)}</span>
              <ExceptionalRefundStatusBadge status={proposal.status} />
            </div>
            <p className="line-clamp-3 text-sm text-foreground">{proposal.reason}</p>
            <p className="text-xs text-muted-foreground">
              {proposal.proposedByName} · {formatDateTime(proposal.proposedAt)}
            </p>
            {canReview && (
              <div className="flex flex-wrap gap-2">
                <Button
                  size="sm"
                  disabled={isOwn}
                  title={isOwn ? "Bạn không thể duyệt đề xuất của chính mình" : undefined}
                  onClick={() => actions.approve(proposal)}
                >
                  Duyệt
                </Button>
                <Button size="sm" variant="outline" onClick={() => actions.reject(proposal)}>
                  Từ chối
                </Button>
              </div>
            )}
          </li>
        );
      })}
      <li>
        <Link
          to={`${ROUTES.admin.refundProposals}?orderId=${orderId}&status=All`}
          className="text-sm text-primary-text underline-offset-4 hover:underline"
        >
          Xem trong danh sách đề xuất
        </Link>
      </li>
    </ul>
  );
};
