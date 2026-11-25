import { History } from "lucide-react";

import { PageHeader } from "@/components/common/page-header";
import { DataTable } from "@/components/common/data-table/data-table";
import { QueryState } from "@/components/common/query-state";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { useDialogClose, useDialogOpen } from "@/stores/dialog-store";
import { useAdminLoyaltyAdjustmentParams } from "@/hooks/use-query-params";
import { useDebouncedValue } from "@/hooks/use-debounced-value";
import { useTablePagination } from "@/hooks/use-table-pagination";
import { DEFAULT_PAGE } from "@/config/default-constants";

import {
  useAdminLoyaltyCustomer,
  useAdminLoyaltyCustomerSearch,
  useAdminLoyaltyCustomerTransactions,
} from "../hooks/use-admin-loyalty-customers";
import { LoyaltyCustomerSearch } from "../components/loyalty-customer-search";
import { LoyaltyCustomerSummary } from "../components/loyalty-customer-summary";
import { AdjustPointsForm } from "../components/forms/adjust-points-form";
import { transactionColumns } from "../components/transaction-columns";
import type { LoyaltyCustomerDetailDto } from "../types";

const AdminLoyaltyAdjustmentsPage = () => {
  const [params, setParams] = useAdminLoyaltyAdjustmentParams();
  const { pagination, onPaginationChange } = useTablePagination(params, setParams);

  const keyword = useDebouncedValue(params.search);
  const search = useAdminLoyaltyCustomerSearch(keyword);
  const customer = useAdminLoyaltyCustomer(params.userId);
  const transactions = useAdminLoyaltyCustomerTransactions(params.userId, {
    page: pagination.pageIndex + 1,
    pageSize: pagination.pageSize,
  });

  const openDialog = useDialogOpen();
  const closeDialog = useDialogClose();

  const handleAdjust = (target: LoyaltyCustomerDetailDto) => {
    openDialog({
      title: "Điều chỉnh điểm",
      description: "Cộng hoặc trừ điểm đổi quà của khách. Mọi điều chỉnh được ghi vào nhật ký thao tác.",
      content: <AdjustPointsForm customer={target} onSuccess={closeDialog} />,
    });
  };

  return (
    <div className="space-y-6">
      <PageHeader
        title="Điều chỉnh điểm"
        description="Tra cứu khách hàng, xem lịch sử điểm và điều chỉnh điểm kèm lý do."
      />

      <LoyaltyCustomerSearch
        value={params.search}
        onChange={(value) => void setParams({ search: value })}
        keyword={keyword}
        selectedUserId={params.userId}
        onSelect={(item) => void setParams({ userId: item.userId, page: DEFAULT_PAGE })}
        results={search.data}
        isLoading={search.isLoading}
        isError={search.isError}
        error={search.error}
        onRetry={() => void search.refetch()}
      />

      {params.userId !== null && (
        <div className="space-y-4">
          <QueryState
            query={customer}
            skeleton={<CardSkeleton className="h-40" />}
            errorTitle="Không tải được thông tin điểm của khách"
          >
            {(data) => (
              <LoyaltyCustomerSummary
                customer={data}
                onAdjust={() => handleAdjust(data)}
                onClear={() => void setParams({ userId: null, page: DEFAULT_PAGE })}
              />
            )}
          </QueryState>

          <DataTable
            columns={transactionColumns}
            data={transactions.data?.items ?? []}
            isLoading={transactions.isLoading}
            isError={transactions.isError}
            error={transactions.error}
            onRetry={() => void transactions.refetch()}
            isRefreshing={transactions.isPlaceholderData}
            totalItems={transactions.data?.totalItems ?? 0}
            pagination={pagination}
            onPaginationChange={onPaginationChange}
            empty={{
              icon: History,
              title: "Chưa có biến động điểm",
              description: "Khách này chưa có giao dịch điểm nào.",
            }}
          />
        </div>
      )}
    </div>
  );
};

export default AdminLoyaltyAdjustmentsPage;
