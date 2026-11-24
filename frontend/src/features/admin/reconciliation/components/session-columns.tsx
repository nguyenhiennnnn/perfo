import { createColumnHelper } from "@tanstack/react-table";
import { Link } from "react-router";
import { Badge } from "@/components/ui/badge";
import type { DataTableFeatures } from "@/components/common/data-table/data-table-features";
import { ROUTES } from "@/config/routes";
import { formatCurrency, formatDate, formatDateTime, formatNumber } from "@/lib/formatters";
import type { ReconciliationSessionDto } from "../types";
import { SESSION_STATUS, getGatewayLabel } from "../utils/reconciliation-labels";

const columnHelper = createColumnHelper<DataTableFeatures, ReconciliationSessionDto>();

const getMismatchCount = (s: ReconciliationSessionDto) =>
  s.amountMismatchCount +
  s.statusMismatchCount +
  s.missingInSystemCount +
  s.missingAtGatewayCount +
  s.paidAtGatewayOrderUnconfirmedCount;

export const createSessionColumns = () =>
  columnHelper.columns([
    columnHelper.accessor("gateway", {
      header: "Cổng",
      cell: ({ row }) => (
        <Link
          to={ROUTES.admin.reconciliationDetail(row.original.sessionId)}
          className="font-medium text-primary-text hover:underline"
        >
          {getGatewayLabel(row.original.gateway)}
          <span className="ml-1 text-xs font-normal text-muted-foreground">
            #{row.original.sessionId}
          </span>
        </Link>
      ),
      enableSorting: false,
    }),
    columnHelper.display({
      id: "period",
      header: "Khoảng ngày / file",
      cell: ({ row }) => {
        const s = row.original;
        if (s.periodFrom && s.periodTo)
          return (
            <span className="text-sm">
              {formatDate(s.periodFrom)} – {formatDate(s.periodTo)}
            </span>
          );
        return <span className="text-sm text-muted-foreground">{s.fileName ?? "—"}</span>;
      },
    }),
    columnHelper.accessor("status", {
      header: "Trạng thái",
      cell: ({ row }) => {
        const item = SESSION_STATUS[row.original.status];
        return item ? (
          <Badge variant={item.variant}>{item.label}</Badge>
        ) : (
          <Badge variant="outline">{row.original.status}</Badge>
        );
      },
      enableSorting: false,
    }),
    columnHelper.display({
      id: "rows",
      header: "Số dòng",
      cell: ({ row }) => {
        const s = row.original;
        const mismatch = getMismatchCount(s);
        return (
          <div className="flex flex-wrap items-center gap-1.5 text-sm tabular-nums">
            <span>{formatNumber(s.totalRows)}</span>
            <Badge variant="success">Khớp {formatNumber(s.matchedCount)}</Badge>
            {mismatch > 0 && <Badge variant="warning">Lệch {formatNumber(mismatch)}</Badge>}
          </div>
        );
      },
    }),
    columnHelper.display({
      id: "amount",
      header: "Tổng tiền (cổng / hệ thống)",
      cell: ({ row }) => (
        <div className="text-sm tabular-nums">
          <p>{formatCurrency(row.original.gatewayTotalAmount)}</p>
          <p className="text-xs text-muted-foreground">
            {formatCurrency(row.original.systemTotalAmount)}
          </p>
        </div>
      ),
    }),
    columnHelper.accessor("createdByName", {
      header: "Người tạo",
      cell: ({ row }) => <span className="text-sm">{row.original.createdByName ?? "—"}</span>,
      enableSorting: false,
    }),
    columnHelper.accessor("createdAt", {
      header: "Thời gian",
      cell: ({ row }) => (
        <span className="text-sm tabular-nums">{formatDateTime(row.original.createdAt)}</span>
      ),
      enableSorting: false,
    }),
  ]);
