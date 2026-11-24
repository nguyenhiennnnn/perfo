import { createColumnHelper } from "@tanstack/react-table";
import { Link } from "react-router";

import { Badge } from "@/components/ui/badge";
import type { DataTableFeatures } from "@/components/common/data-table/data-table-features";
import { ROUTES } from "@/config/routes";
import { formatCurrency, formatDate } from "@/lib/formatters";

import type { UnclaimedComboOrderDto } from "../types";

const columnHelper = createColumnHelper<DataTableFeatures, UnclaimedComboOrderDto>();

export const createUnclaimedComboColumns = () =>
  columnHelper.columns([
    columnHelper.display({
      id: "orderCode",
      header: "Mã đơn",
      cell: ({ row }) => (
        <Link
          to={ROUTES.admin.orderDetail(row.original.orderId)}
          className="t-code text-primary-text underline-offset-4 hover:underline"
        >
          {row.original.orderCode}
        </Link>
      ),
    }),
    columnHelper.display({
      id: "customer",
      header: "Khách hàng",
      cell: ({ row }) => (
        <div className="flex flex-col">
          <span className="font-medium text-foreground">{row.original.customerName || "—"}</span>
          {row.original.customerPhone && (
            <span className="text-xs text-muted-foreground">{row.original.customerPhone}</span>
          )}
        </div>
      ),
    }),
    columnHelper.display({
      id: "saleDate",
      header: "Ngày bán",
      cell: ({ row }) => (
        <div className="flex flex-col">
          <span className="tabular-nums">{formatDate(row.original.saleDate)}</span>
          <Badge variant="warning" className="mt-1 w-fit">
            Quá hạn {row.original.daysOverdue} ngày
          </Badge>
        </div>
      ),
    }),
    columnHelper.display({
      id: "lines",
      header: "Combo chưa nhận",
      cell: ({ row }) => (
        <ul className="space-y-0.5 text-sm">
          {row.original.lines
            .filter((line) => line.remainingQuantity > 0)
            .map((line, index) => (
              <li key={`${line.comboName}-${index}`}>
                {line.comboName ?? "Combo"}{" "}
                <span className="tabular-nums text-muted-foreground">
                  còn {line.remainingQuantity}/{line.quantity}
                </span>
              </li>
            ))}
        </ul>
      ),
    }),
    columnHelper.display({
      id: "progress",
      header: "Đã giao",
      cell: ({ row }) => (
        <span className="tabular-nums">
          {row.original.fulfilledQuantity}/{row.original.totalQuantity}
        </span>
      ),
    }),
    columnHelper.display({
      id: "amount",
      header: "Giá trị đơn",
      cell: ({ row }) => <span className="tabular-nums">{formatCurrency(row.original.finalAmount)}</span>,
    }),
  ]);
