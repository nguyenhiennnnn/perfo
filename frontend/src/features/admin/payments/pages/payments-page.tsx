import { CreditCard } from "lucide-react";

import { ENDPOINTS } from "@/api/endpoints";
import { DataTable } from "@/components/common/data-table/data-table";
import { DateFilterPicker } from "@/components/common/filter/date-filter-picker";
import { EnumSelectFilter } from "@/components/common/filter/enum-select-filter";
import { ExportButton } from "@/components/common/export-button";
import { PageHeader } from "@/components/common/page-header";
import { Checkbox } from "@/components/ui/checkbox";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { ALL_STATUSES_LABEL, DEFAULT_PAGE, STATUS_LABEL } from "@/config/default-constants";
import { useDebouncedValue } from "@/hooks/use-debounced-value";
import { useAdminPaymentParams } from "@/hooks/use-query-params";
import { useTablePagination } from "@/hooks/use-table-pagination";
import { paymentMethodOptions } from "@/lib/enum-options";
import { stripNulls } from "@/lib/query-params";
import { useDialogOpen } from "@/stores/dialog-store";

import { createPaymentColumns } from "../components/payment-columns";
import { PaymentDetailView } from "../components/payment-detail-view";
import { useAdminPaymentTransactions } from "../hooks/use-payment-ledger";
import type { AdminPaymentListItemDto } from "../types/ledger";
import { transactionStatusOptions, transactionTypeOptions } from "../utils/transaction-labels";

const AdminPaymentsPage = () => {
  const [params, setParams] = useAdminPaymentParams();
  const { pagination, onPaginationChange } = useTablePagination(params, setParams);
  const openDialog = useDialogOpen();

  const debouncedSearch = useDebouncedValue(params.search);
  const debouncedOrderCode = useDebouncedValue(params.orderCode);

  const filters = stripNulls({
    search: debouncedSearch || undefined,
    orderCode: debouncedOrderCode || undefined,
    type: params.type,
    status: params.status,
    paymentMethod: params.paymentMethod,
    needsRefund: params.needsRefund ? true : undefined,
    fromDate: params.fromDate,
    toDate: params.toDate,
  });
  const payments = useAdminPaymentTransactions({
    ...filters,
    page: params.page,
    pageSize: params.pageSize,
  });

  const handleViewDetail = (payment: AdminPaymentListItemDto) => {
    openDialog({
      title: `Giao dịch ${payment.transactionCode}`,
      description: `Chi tiết sổ giao dịch của đơn ${payment.orderCode}.`,
      size: "lg",
      content: <PaymentDetailView transactionId={payment.transactionId} />,
    });
  };

  const columns = createPaymentColumns({ onViewDetail: handleViewDetail });

  const hasActiveFilters =
    Boolean(params.search) ||
    Boolean(params.orderCode) ||
    params.type !== null ||
    params.status !== null ||
    params.paymentMethod !== null ||
    params.needsRefund === true ||
    params.fromDate !== null ||
    params.toDate !== null;

  return (
    <div>
      <PageHeader
        title="Thanh toán"
        description="Sổ giao dịch thu và hoàn tiền của mọi đơn hàng."
        action={
          <ExportButton
            endpoint={ENDPOINTS.admin.payments.export}
            jobEndpoint={ENDPOINTS.admin.payments.exportJobs}
            params={filters}
            fallbackFilename="payments"
            size="default"
          />
        }
      />

      <div className="mb-4 grid grid-cols-2 gap-3 md:grid-cols-3 xl:grid-cols-4">
        <Input
          placeholder="Tìm mã giao dịch, mã cổng..."
          value={params.search}
          onChange={(e) => setParams({ search: e.target.value, page: DEFAULT_PAGE })}
          className="col-span-2 md:col-span-1"
        />
        <Input
          placeholder="Mã đơn..."
          value={params.orderCode}
          onChange={(e) => setParams({ orderCode: e.target.value, page: DEFAULT_PAGE })}
          className="col-span-2 md:col-span-1"
        />
        <EnumSelectFilter
          value={params.type}
          onChange={(type) => setParams({ type, page: DEFAULT_PAGE })}
          options={transactionTypeOptions}
          placeholder="Loại giao dịch"
          allLabel="Thu và hoàn"
        />
        <EnumSelectFilter
          value={params.paymentMethod}
          onChange={(paymentMethod) => setParams({ paymentMethod, page: DEFAULT_PAGE })}
          options={paymentMethodOptions}
          placeholder="Phương thức"
          allLabel="Tất cả phương thức"
        />
        <EnumSelectFilter
          value={params.status}
          onChange={(status) => setParams({ status, page: DEFAULT_PAGE })}
          options={transactionStatusOptions}
          placeholder={STATUS_LABEL}
          allLabel={ALL_STATUSES_LABEL}
        />
        <DateFilterPicker
          value={params.fromDate}
          onChange={(fromDate) => setParams({ fromDate, page: DEFAULT_PAGE })}
          placeholder="Từ ngày"
        />
        <DateFilterPicker
          value={params.toDate}
          onChange={(toDate) => setParams({ toDate, page: DEFAULT_PAGE })}
          placeholder="Đến ngày"
        />
        <Label className="col-span-2 flex items-center gap-2 text-sm text-foreground md:col-span-1">
          <Checkbox
            checked={params.needsRefund === true}
            onCheckedChange={(checked) =>
              setParams({ needsRefund: checked ? true : null, page: DEFAULT_PAGE })
            }
          />
          Cần hoàn tiền mặt
        </Label>
      </div>

      <DataTable
        columns={columns}
        data={payments.data?.items ?? []}
        isLoading={payments.isLoading}
        isError={payments.isError}
        error={payments.error}
        onRetry={() => void payments.refetch()}
        isRefreshing={payments.isPlaceholderData}
        totalItems={payments.data?.totalItems ?? 0}
        pagination={pagination}
        onPaginationChange={onPaginationChange}
        empty={{
          icon: CreditCard,
          title: "Chưa có giao dịch",
          description: "Giao dịch thu và hoàn hiện ở đây sau khi đơn hàng phát sinh thanh toán.",
        }}
        hasActiveFilters={hasActiveFilters}
        onClearFilters={() =>
          setParams({
            search: "",
            orderCode: "",
            type: null,
            status: null,
            paymentMethod: null,
            needsRefund: null,
            fromDate: null,
            toDate: null,
            page: DEFAULT_PAGE,
          })
        }
      />
    </div>
  );
};

export default AdminPaymentsPage;
