import { createColumnHelper } from "@tanstack/react-table";
import { Link } from "react-router";
import { Badge } from "@/components/ui/badge";
import { StaffSupportRequestStatusBadge } from "@/components/common/status-badge";
import { DataTableColumnHeader } from "@/components/common/data-table/data-table-column-header";
import type { DataTableFeatures } from "@/components/common/data-table/data-table-features";
import { STATUS_LABEL } from "@/config/default-constants";
import { ROUTES } from "@/config/routes";
import { formatDateTime } from "@/lib/formatters";
import { SupportRequestStatus } from "@/types/enum";
import type { StaffSupportRequestListItemDto } from "../types";
import { getStaffSupportTypeLabel } from "../utils/support-labels";

const columnHelper = createColumnHelper<DataTableFeatures, StaffSupportRequestListItemDto>();

export const createSupportRequestColumns = () =>
  columnHelper.columns([
    columnHelper.accessor("subject", {
      enableSorting: false,
      header: ({ column }) => <DataTableColumnHeader column={column} title="Yêu cầu" />,
      cell: ({ row }) => (
        <div className="flex min-w-48 max-w-80 flex-col">
          <Link
            to={ROUTES.admin.supportRequestDetail(row.original.supportRequestId)}
            className="truncate font-medium text-foreground underline-offset-4 hover:underline"
          >
            {row.original.subject}
          </Link>
          <span className="text-xs text-muted-foreground">{getStaffSupportTypeLabel(row.original.type)}</span>
        </div>
      ),
    }),
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
    columnHelper.accessor("customerName", {
      enableSorting: false,
      header: ({ column }) => <DataTableColumnHeader column={column} title="Khách hàng" />,
      cell: ({ row }) => (
        <span className="text-sm text-foreground">{row.original.customerName || "Khách vãng lai"}</span>
      ),
    }),
    columnHelper.accessor("status", {
      enableSorting: false,
      header: ({ column }) => <DataTableColumnHeader column={column} title={STATUS_LABEL} />,
      cell: ({ row }) => <StaffSupportRequestStatusBadge status={row.original.status} />,
    }),
    columnHelper.accessor("assigneeName", {
      enableSorting: false,
      header: ({ column }) => <DataTableColumnHeader column={column} title="Người phụ trách" />,
      cell: ({ row }) =>
        row.original.assigneeName ? (
          <span className="text-sm text-foreground">{row.original.assigneeName}</span>
        ) : (
          <span className="text-sm text-muted-foreground">Chưa gán</span>
        ),
    }),
    columnHelper.accessor("dueAt", {
      enableSorting: false,
      header: ({ column }) => <DataTableColumnHeader column={column} title="Hạn xử lý" />,
      cell: ({ row }) => (
        <div className="flex min-w-32 flex-col items-start gap-1">
          <span className="text-sm tabular-nums text-muted-foreground">{formatDateTime(row.original.dueAt)}</span>
          {row.original.isOverdue && row.original.status !== SupportRequestStatus.Resolved && (
            <Badge variant="destructive">Quá hạn</Badge>
          )}
        </div>
      ),
    }),
    columnHelper.accessor("lastMessageAt", {
      enableSorting: false,
      header: ({ column }) => <DataTableColumnHeader column={column} title="Cập nhật" />,
      cell: ({ row }) => (
        <span className="text-sm tabular-nums text-muted-foreground">
          {formatDateTime(row.original.lastMessageAt)}
        </span>
      ),
    }),
  ]);
