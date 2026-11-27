import { createColumnHelper } from "@tanstack/react-table";
import { HandCoins, MoreHorizontal } from "lucide-react";
import { Link } from "react-router";

import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { DataTableColumnHeader } from "@/components/common/data-table/data-table-column-header";
import type { DataTableFeatures } from "@/components/common/data-table/data-table-features";
import { ROUTES } from "@/config/routes";
import { STATUS_LABEL } from "@/config/default-constants";
import { formatCurrency, formatDate } from "@/lib/formatters";
import { MembershipStatusBadge } from "@/features/client/memberships/components/membership-status-badge";
import { getSubscriptionState } from "@/features/client/memberships/utils/membership-plans";
import { MembershipSubscriptionStatus } from "@/types/enum";

import type { AdminMembershipSubscriptionDto } from "../types";
import { getRefundReasonLabel } from "../utils/membership-plan-admin-labels";

interface MembershipSubscriptionColumnsOptions {
  onRefund: (subscription: AdminMembershipSubscriptionDto) => void;
}

const columnHelper = createColumnHelper<DataTableFeatures, AdminMembershipSubscriptionDto>();

export const createMembershipSubscriptionColumns = ({
  onRefund,
}: MembershipSubscriptionColumnsOptions) =>
  columnHelper.columns([
    columnHelper.accessor("customerName", {
      header: ({ column }) => <DataTableColumnHeader column={column} title="Hội viên" />,
      cell: ({ row }) => (
        <div className="flex max-w-56 flex-col gap-0.5">
          <span className="truncate font-medium text-foreground">{row.original.customerName}</span>
          <span className="truncate text-xs text-muted-foreground">
            {row.original.customerEmail}
          </span>
          {row.original.customerPhone && (
            <span className="text-xs tabular-nums text-muted-foreground">
              {row.original.customerPhone}
            </span>
          )}
        </div>
      ),
      enableSorting: false,
    }),
    columnHelper.display({
      id: "plan",
      header: "Gói",
      cell: ({ row }) => (
        <div className="flex flex-col gap-0.5">
          <span className="text-sm font-medium text-foreground">{row.original.planName}</span>
          <span className="text-xs tabular-nums text-muted-foreground">
            {formatCurrency(row.original.planPrice)}
          </span>
        </div>
      ),
      enableSorting: false,
    }),
    columnHelper.display({
      id: "order",
      header: "Đơn mua",
      cell: ({ row }) => (
        <Link
          to={ROUTES.admin.orderDetail(row.original.orderId)}
          className="t-code text-sm font-medium text-primary-text hover:underline"
        >
          {row.original.orderCode}
        </Link>
      ),
      enableSorting: false,
    }),
    columnHelper.display({
      id: "validity",
      header: "Hiệu lực",
      cell: ({ row }) => (
        <span className="text-sm tabular-nums text-muted-foreground">
          {formatDate(row.original.validFrom)} - {formatDate(row.original.validTo)}
        </span>
      ),
      enableSorting: false,
    }),
    columnHelper.display({
      id: "status",
      header: () => STATUS_LABEL,
      cell: ({ row }) => {
        const subscription = row.original;
        return (
          <div className="flex flex-col items-start gap-1">
            <MembershipStatusBadge state={getSubscriptionState(subscription, Date.now())} />
            {subscription.status === MembershipSubscriptionStatus.Refunded && (
              <span className="text-xs text-muted-foreground">
                {getRefundReasonLabel(subscription.refundReason)}
                {subscription.refundedAt ? ` · ${formatDate(subscription.refundedAt)}` : ""}
              </span>
            )}
          </div>
        );
      },
      enableSorting: false,
    }),
    columnHelper.display({
      id: "stopped",
      header: "Doanh thu đã dừng",
      cell: ({ row }) =>
        row.original.status === MembershipSubscriptionStatus.Refunded ? (
          <span className="text-sm tabular-nums">
            {formatCurrency(row.original.stoppedRevenueAmount)}
          </span>
        ) : (
          <span className="text-sm text-muted-foreground">-</span>
        ),
      enableSorting: false,
    }),
    columnHelper.display({
      id: "actions",
      header: () => <div className="text-right">Hành động</div>,
      cell: ({ row }) =>
        row.original.status === MembershipSubscriptionStatus.Active ? (
          <div className="flex justify-end">
            <DropdownMenu>
              <DropdownMenuTrigger>
                <Button variant="ghost" size="icon" className="size-8">
                  <span className="sr-only">Mở menu</span>
                  <MoreHorizontal className="size-4" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                <DropdownMenuItem variant="destructive" onClick={() => onRefund(row.original)}>
                  <HandCoins /> Hoàn ngoại lệ
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        ) : null,
      enableHiding: false,
    }),
  ]);
