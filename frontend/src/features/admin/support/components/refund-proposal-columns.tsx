import { createColumnHelper } from "@tanstack/react-table";
import { Link } from "react-router";
import { ExceptionalRefundStatusBadge } from "@/components/common/status-badge";
import { DataTableColumnHeader } from "@/components/common/data-table/data-table-column-header";
import type { DataTableFeatures } from "@/components/common/data-table/data-table-features";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { STATUS_LABEL } from "@/config/default-constants";
import { ROUTES } from "@/config/routes";
import { formatCurrency, formatDateTime } from "@/lib/formatters";
import { ExceptionalRefundStatus } from "@/types/enum";
import type { ExceptionalRefundProposalDto } from "../types";

interface RefundProposalColumnsOptions {
  canReview: boolean;
  currentUserId?: number;
  onApprove: (proposal: ExceptionalRefundProposalDto) => void;
  onReject: (proposal: ExceptionalRefundProposalDto) => void;
}

const columnHelper = createColumnHelper<DataTableFeatures, ExceptionalRefundProposalDto>();

export const createRefundProposalColumns = ({
  canReview,
  currentUserId,
  onApprove,
  onReject,
}: RefundProposalColumnsOptions) =>
  columnHelper.columns([
    columnHelper.accessor("orderCode", {
      enableSorting: false,
      header: ({ column }) => <DataTableColumnHeader column={column} title="Mã đơn" />,
      cell: ({ row }) => (
        <Link
          to={ROUTES.admin.orderDetail(row.original.orderId)}
          className="t-code text-foreground underline-offset-4 hover:underline"
        >
          {row.original.orderCode}
        </Link>
      ),
    }),
    columnHelper.accessor("amount", {
      enableSorting: false,
      header: ({ column }) => <DataTableColumnHeader column={column} title="Số tiền hoàn" />,
      cell: ({ row }) => (
        <div className="flex min-w-32 flex-col items-start gap-1">
          <span className="font-semibold tabular-nums text-primary-text">{formatCurrency(row.original.amount)}</span>
          {row.original.requiresAdminApproval && <Badge variant="warning">Vượt ngưỡng</Badge>}
        </div>
      ),
    }),
    columnHelper.accessor("orderLineDescription", {
      enableSorting: false,
      header: ({ column }) => <DataTableColumnHeader column={column} title="Hoàn cho" />,
      cell: ({ row }) => (
        <span className="text-sm text-foreground">{row.original.orderLineDescription ?? "Toàn đơn"}</span>
      ),
    }),
    columnHelper.accessor("reason", {
      enableSorting: false,
      header: ({ column }) => <DataTableColumnHeader column={column} title="Lý do" />,
      cell: ({ row }) => (
        <p className="line-clamp-2 min-w-48 max-w-72 text-sm text-foreground" title={row.original.reason}>
          {row.original.reason}
        </p>
      ),
    }),
    columnHelper.accessor("proposedByName", {
      enableSorting: false,
      header: ({ column }) => <DataTableColumnHeader column={column} title="Người đề xuất" />,
      cell: ({ row }) => (
        <div className="flex min-w-32 flex-col">
          <span className="text-sm text-foreground">{row.original.proposedByName}</span>
          <span className="text-xs tabular-nums text-muted-foreground">
            {formatDateTime(row.original.proposedAt)}
          </span>
        </div>
      ),
    }),
    columnHelper.accessor("status", {
      enableSorting: false,
      header: ({ column }) => <DataTableColumnHeader column={column} title={STATUS_LABEL} />,
      cell: ({ row }) => <ExceptionalRefundStatusBadge status={row.original.status} />,
    }),
    columnHelper.accessor("reviewedByName", {
      enableSorting: false,
      header: ({ column }) => <DataTableColumnHeader column={column} title="Người duyệt" />,
      cell: ({ row }) =>
        row.original.reviewedByName ? (
          <div className="flex min-w-32 flex-col">
            <span className="text-sm text-foreground">{row.original.reviewedByName}</span>
            <span className="text-xs tabular-nums text-muted-foreground">
              {formatDateTime(row.original.reviewedAt)}
            </span>
            {row.original.reviewNote && (
              <span className="line-clamp-2 max-w-56 text-xs text-muted-foreground" title={row.original.reviewNote}>
                {row.original.reviewNote}
              </span>
            )}
          </div>
        ) : (
          <span className="text-sm text-muted-foreground">—</span>
        ),
    }),
    columnHelper.display({
      id: "actions",
      header: () => <div className="text-right">Hành động</div>,
      cell: ({ row }) => {
        const proposal = row.original;
        if (!canReview || proposal.status !== ExceptionalRefundStatus.Pending) return null;
        const isOwn = proposal.proposedByUserId === currentUserId;
        return (
          <div className="flex justify-end gap-2">
            <Button
              size="sm"
              disabled={isOwn}
              title={isOwn ? "Bạn không thể duyệt đề xuất của chính mình" : undefined}
              onClick={() => onApprove(proposal)}
            >
              Duyệt
            </Button>
            <Button size="sm" variant="outline" onClick={() => onReject(proposal)}>
              Từ chối
            </Button>
          </div>
        );
      },
      enableHiding: false,
    }),
  ]);
