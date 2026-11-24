import { createColumnHelper } from "@tanstack/react-table";
import { CheckCircle2, FileUp, Wrench, XCircle } from "lucide-react";
import { Link } from "react-router";
import { DataTableColumnHeader } from "@/components/common/data-table/data-table-column-header";
import type { DataTableFeatures } from "@/components/common/data-table/data-table-features";
import { DownloadTicketsPdfButton } from "@/components/common/download-tickets-pdf-button";
import { InvoiceRequestStatusBadge } from "@/components/common/status-badge";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { ENDPOINTS } from "@/api/endpoints";
import { ROUTES } from "@/config/routes";
import type { InvoiceRequestDto } from "@/features/client/invoices/types";
import { formatCurrency, formatDateTime } from "@/lib/formatters";
import { InvoiceRequestStatus } from "@/types/enum";

interface InvoiceRequestColumnsOptions {
  onIssue: (request: InvoiceRequestDto) => void;
  onReject: (request: InvoiceRequestDto) => void;
  onReplaceFile: (request: InvoiceRequestDto) => void;
  onResolveAdjustment: (request: InvoiceRequestDto) => void;
}

const columnHelper = createColumnHelper<DataTableFeatures, InvoiceRequestDto>();

export const createInvoiceRequestColumns = ({
  onIssue,
  onReject,
  onReplaceFile,
  onResolveAdjustment,
}: InvoiceRequestColumnsOptions) =>
  columnHelper.columns([
    columnHelper.accessor("orderCode", {
      header: ({ column }) => <DataTableColumnHeader column={column} title="Đơn hàng" />,
      cell: ({ row }) => (
        <div className="flex flex-col">
          <Link
            to={ROUTES.admin.orderDetail(row.original.orderId)}
            className="t-code text-sm font-medium text-primary-text hover:underline"
          >
            {row.original.orderCode}
          </Link>
          <span className="text-xs tabular-nums text-muted-foreground">
            {formatDateTime(row.original.createdAt)}
          </span>
        </div>
      ),
      enableSorting: false,
    }),
    columnHelper.accessor("companyName", {
      header: ({ column }) => <DataTableColumnHeader column={column} title="Đơn vị" />,
      cell: ({ row }) => (
        <div className="flex min-w-48 flex-col">
          <span className="text-sm font-medium wrap-break-word text-foreground">{row.original.companyName}</span>
          <span className="t-code text-xs text-muted-foreground">MST {row.original.taxCode}</span>
          <span className="text-xs break-all text-muted-foreground">{row.original.email}</span>
        </div>
      ),
      enableSorting: false,
    }),
    columnHelper.accessor("status", {
      header: ({ column }) => <DataTableColumnHeader column={column} title="Trạng thái" />,
      cell: ({ row }) => {
        const request = row.original;
        return (
          <div className="flex flex-col items-start gap-1">
            <InvoiceRequestStatusBadge status={request.status} />
            {request.needsAdjustment && <Badge variant="warning">Cần điều chỉnh</Badge>}
            {request.status === InvoiceRequestStatus.Rejected && request.rejectReason && (
              <span className="max-w-56 text-xs wrap-break-word text-muted-foreground">{request.rejectReason}</span>
            )}
          </div>
        );
      },
      enableSorting: false,
    }),
    columnHelper.accessor("invoiceNumber", {
      header: ({ column }) => <DataTableColumnHeader column={column} title="Số hóa đơn" />,
      cell: ({ row }) => (
        <div className="flex flex-col">
          <span className="t-code text-sm text-foreground">{row.original.invoiceNumber ?? "—"}</span>
          {row.original.processedAt && (
            <span className="text-xs tabular-nums text-muted-foreground">
              {formatDateTime(row.original.processedAt)}
            </span>
          )}
        </div>
      ),
      enableSorting: false,
    }),
    columnHelper.accessor("adjustmentRefundAmount", {
      header: ({ column }) => <DataTableColumnHeader column={column} title="Hoàn sau xuất" />,
      cell: ({ row }) =>
        row.original.adjustmentRefundAmount > 0 ? (
          <div className="flex flex-col">
            <span className="text-sm tabular-nums text-foreground">
              {formatCurrency(row.original.adjustmentRefundAmount)}
            </span>
            {row.original.adjustmentFlaggedAt && (
              <span className="text-xs tabular-nums text-muted-foreground">
                {formatDateTime(row.original.adjustmentFlaggedAt)}
              </span>
            )}
            {row.original.adjustmentNote && (
              <span className="max-w-56 text-xs wrap-break-word text-muted-foreground">
                {row.original.adjustmentNote}
              </span>
            )}
          </div>
        ) : (
          <span className="text-sm text-muted-foreground">—</span>
        ),
      enableSorting: false,
    }),
    columnHelper.display({
      id: "actions",
      header: () => <div className="text-right">Hành động</div>,
      cell: ({ row }) => {
        const request = row.original;
        const isPending = request.status === InvoiceRequestStatus.Pending;
        const isIssued = request.status === InvoiceRequestStatus.Issued;
        return (
          <div className="flex flex-wrap justify-end gap-1">
            {isPending && (
              <>
                <Button size="sm" onClick={() => onIssue(request)}>
                  <CheckCircle2 className="size-4" />
                  <span>Xuất hóa đơn</span>
                </Button>
                <Button size="sm" variant="outline" onClick={() => onReject(request)}>
                  <XCircle className="size-4" />
                  <span>Từ chối</span>
                </Button>
              </>
            )}
            {isIssued && request.hasFile && (
              <DownloadTicketsPdfButton
                endpoint={ENDPOINTS.admin.invoiceRequests.file(request.invoiceRequestId)}
                fallbackFilename={`hoa-don-${request.orderCode}.pdf`}
                label="Tải tệp"
                errorTitle="Không thể tải hóa đơn"
              />
            )}
            {isIssued && (
              <Button size="sm" variant="outline" onClick={() => onReplaceFile(request)}>
                <FileUp className="size-4" />
                <span>{request.hasFile ? "Thay tệp" : "Đính kèm tệp"}</span>
              </Button>
            )}
            {request.needsAdjustment && (
              <Button size="sm" variant="secondary" onClick={() => onResolveAdjustment(request)}>
                <Wrench className="size-4" />
                <span>Đã điều chỉnh</span>
              </Button>
            )}
          </div>
        );
      },
    }),
  ]);
