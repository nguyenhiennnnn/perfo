import { FileText } from "lucide-react";
import { DataTable } from "@/components/common/data-table/data-table";
import { EnumSelectFilter } from "@/components/common/filter/enum-select-filter";
import { PageHeader } from "@/components/common/page-header";
import { Input } from "@/components/ui/input";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { DEFAULT_PAGE } from "@/config/default-constants";
import { useAdminInvoiceRequestParams } from "@/hooks/use-query-params";
import { useTablePagination } from "@/hooks/use-table-pagination";
import type { InvoiceRequestDto } from "@/features/client/invoices/types";
import { useDialogOpen } from "@/stores/dialog-store";
import { createInvoiceRequestColumns } from "../components/invoice-request-columns";
import { IssueInvoiceDialogContent } from "../components/issue-invoice-dialog-content";
import { RejectInvoiceDialogContent } from "../components/reject-invoice-dialog-content";
import { ReplaceInvoiceFileDialogContent } from "../components/replace-invoice-file-dialog-content";
import { ResolveAdjustmentDialogContent } from "../components/resolve-adjustment-dialog-content";
import { useAdminInvoiceRequests } from "../hooks/use-admin-invoices";
import { INVOICE_STATUS_OPTIONS } from "../utils/invoice-status-options";

const AdminInvoiceRequestsPage = () => {
  const [params, setParams] = useAdminInvoiceRequestParams();
  const { pagination, onPaginationChange } = useTablePagination(params, setParams);
  const openDialog = useDialogOpen();
  const isAdjustmentTab = params.tab === "adjustments";

  const requests = useAdminInvoiceRequests({
    page: params.page,
    pageSize: params.pageSize,
    search: params.search || undefined,
    ...(isAdjustmentTab
      ? { needsAdjustment: true }
      : { status: params.status ?? undefined }),
  });

  const columns = createInvoiceRequestColumns({
    onIssue: (request: InvoiceRequestDto) =>
      openDialog({
        title: "Xuất hóa đơn",
        description: `Đơn ${request.orderCode} - ${request.companyName} (MST ${request.taxCode})`,
        content: <IssueInvoiceDialogContent requestId={request.invoiceRequestId} />,
      }),
    onReject: (request) =>
      openDialog({
        title: "Từ chối yêu cầu hóa đơn",
        description: `Đơn ${request.orderCode} - ${request.companyName}`,
        content: <RejectInvoiceDialogContent requestId={request.invoiceRequestId} />,
      }),
    onReplaceFile: (request) =>
      openDialog({
        title: request.hasFile ? "Thay tệp hóa đơn" : "Đính kèm tệp hóa đơn",
        description: `Hóa đơn ${request.invoiceNumber ?? ""} - đơn ${request.orderCode}`,
        content: <ReplaceInvoiceFileDialogContent requestId={request.invoiceRequestId} />,
      }),
    onResolveAdjustment: (request) =>
      openDialog({
        title: "Xử lý điều chỉnh hóa đơn",
        description: `Hóa đơn ${request.invoiceNumber ?? ""} - đơn ${request.orderCode}`,
        content: (
          <ResolveAdjustmentDialogContent
            requestId={request.invoiceRequestId}
            refundAmount={request.adjustmentRefundAmount}
          />
        ),
      }),
  });

  const hasActiveFilters = Boolean(params.search) || (!isAdjustmentTab && params.status !== null);

  return (
    <div className="space-y-4">
      <PageHeader
        title="Yêu cầu hóa đơn VAT"
        description="Xử lý yêu cầu xuất hóa đơn của khách và các đơn cần điều chỉnh hóa đơn sau hoàn tiền."
      />

      <Tabs
        value={params.tab}
        onValueChange={(tab) =>
          void setParams({ tab: tab as "requests" | "adjustments", page: DEFAULT_PAGE })
        }
      >
        <TabsList>
          <TabsTrigger value="requests">Yêu cầu hóa đơn</TabsTrigger>
          <TabsTrigger value="adjustments">Cần điều chỉnh</TabsTrigger>
        </TabsList>
      </Tabs>

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
        <Input
          placeholder="Mã đơn, mã số thuế, tên đơn vị, số hóa đơn..."
          value={params.search}
          onChange={(e) => void setParams({ search: e.target.value, page: DEFAULT_PAGE })}
        />
        {!isAdjustmentTab && (
          <EnumSelectFilter
            value={params.status}
            onChange={(status) => void setParams({ status, page: DEFAULT_PAGE })}
            options={INVOICE_STATUS_OPTIONS}
            placeholder="Trạng thái"
            allLabel="Tất cả trạng thái"
          />
        )}
      </div>

      <DataTable
        columns={columns}
        data={requests.data?.items ?? []}
        isLoading={requests.isLoading}
        isError={requests.isError}
        error={requests.error}
        onRetry={() => void requests.refetch()}
        isRefreshing={requests.isPlaceholderData}
        totalItems={requests.data?.totalItems ?? 0}
        pagination={pagination}
        onPaginationChange={onPaginationChange}
        empty={{
          icon: FileText,
          title: isAdjustmentTab ? "Không có hóa đơn cần điều chỉnh" : "Chưa có yêu cầu hóa đơn",
          description: isAdjustmentTab
            ? "Đơn đã xuất hóa đơn rồi hoàn tiền sẽ xuất hiện tại đây."
            : "Yêu cầu xuất hóa đơn của khách và quầy sẽ xuất hiện tại đây.",
        }}
        hasActiveFilters={hasActiveFilters}
        onClearFilters={() => void setParams({ search: "", status: null, page: DEFAULT_PAGE })}
      />
    </div>
  );
};

export default AdminInvoiceRequestsPage;
