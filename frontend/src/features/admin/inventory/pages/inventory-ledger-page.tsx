import { BookOpen } from "lucide-react";

import { PageHeader } from "@/components/common/page-header";
import { Badge } from "@/components/ui/badge";
import { DataTable } from "@/components/common/data-table/data-table";
import { DateFilterPicker } from "@/components/common/filter/date-filter-picker";
import { EmptyState } from "@/components/common/empty-state";
import { SelectField } from "@/components/common/select-field";
import { DEFAULT_PAGE } from "@/config/default-constants";
import { useAdminInventoryLedgerParams } from "@/hooks/use-query-params";
import { useTablePagination } from "@/hooks/use-table-pagination";
import { getBusinessToday } from "@/lib/business-time";
import { formatDate } from "@/lib/formatters";

import { createStockLedgerColumns } from "../components/stock-ledger-columns";
import { useAdminInventoryItemOptions, useAdminStockLedger } from "../hooks/use-admin-inventory";
import type { StockLedgerResponse } from "../types";
import { QUANTITY_EPSILON, formatQuantity } from "../utils/inventory-labels";
import { FilterBar } from "@/components/common/filter/filter-bar";

const SummaryFigure = ({ label, value }: { label: string; value: string }) => (
  <div className="bg-card p-4 cut-md">
    <p className="t-h3 tabular-nums text-foreground">{value}</p>
    <p className="mt-1 text-xs text-muted-foreground">{label}</p>
  </div>
);

const ReconciliationNotice = ({
  ledger,
  toDate,
}: {
  ledger: StockLedgerResponse;
  toDate: string | null;
}) => {
  const isCurrent = !toDate || toDate >= getBusinessToday();
  const onHand = formatQuantity(ledger.quantityOnHand, ledger.unit);

  if (!isCurrent) {
    return (
      <p className="text-sm text-muted-foreground">
        Số dư cuối kỳ tính đến hết ngày {formatDate(toDate)}. Tồn hiện tại của sản phẩm là {onHand}.
      </p>
    );
  }

  const matches = Math.abs(ledger.closingBalance - ledger.quantityOnHand) < QUANTITY_EPSILON;

  return (
    <div className="flex flex-wrap items-center gap-2">
      <Badge variant={matches ? "success" : "destructive"}>
        {matches ? "Sổ cái khớp tồn kho" : "Sổ cái lệch tồn kho"}
      </Badge>
      <span className="text-sm text-muted-foreground">
        Số dư cuối kỳ {formatQuantity(ledger.closingBalance, ledger.unit)}, tồn hiện tại {onHand}
      </span>
    </div>
  );
};

const AdminInventoryLedgerPage = () => {
  const [params, setParams] = useAdminInventoryLedgerParams();
  const { pagination, onPaginationChange } = useTablePagination(params, setParams);

  const items = useAdminInventoryItemOptions();
  const ledger = useAdminStockLedger({
    enabled: params.itemId !== null,
    inventoryItemId: params.itemId ?? undefined,
    fromDate: params.fromDate ?? undefined,
    toDate: params.toDate ?? undefined,
    page: pagination.pageIndex + 1,
    pageSize: pagination.pageSize,
  });

  const ledgerData = ledger.data;
  const columns = createStockLedgerColumns(ledgerData?.unit ?? "");

  return (
    <div>
      <PageHeader
        title="Sổ cái kho"
        description="Mọi biến động nhập, bán, hoàn, kiểm kê và hao hụt của một sản phẩm kho; tồn là kết quả cộng dồn."
      />

      <FilterBar className="mb-4">
        <SelectField
          value={params.itemId}
          onChange={(value) => setParams({ itemId: value ? Number(value) : null, page: DEFAULT_PAGE })}
          options={items.data ?? []}
          getOptionValue={(item) => item.inventoryItemId}
          getOptionLabel={(item) => `${item.name} (${item.code})`}
          placeholder="Chọn sản phẩm kho"
          className="sm:w-72"
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
      </FilterBar>

      {params.itemId === null ? (
        <EmptyState
          icon={BookOpen}
          title="Chọn sản phẩm kho để xem sổ cái"
          description="Sổ cái hiển thị theo từng sản phẩm và khoảng thời gian."
        />
      ) : (
        <div className="space-y-4">
          {ledgerData && (
            <>
              <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
                <SummaryFigure
                  label="Tồn đầu kỳ"
                  value={formatQuantity(ledgerData.openingBalance, ledgerData.unit)}
                />
                <SummaryFigure
                  label="Tổng nhập trong kỳ"
                  value={formatQuantity(Math.abs(ledgerData.totalIn), ledgerData.unit)}
                />
                <SummaryFigure
                  label="Tổng xuất trong kỳ"
                  value={formatQuantity(Math.abs(ledgerData.totalOut), ledgerData.unit)}
                />
                <SummaryFigure
                  label="Tồn cuối kỳ"
                  value={formatQuantity(ledgerData.closingBalance, ledgerData.unit)}
                />
              </div>
              <ReconciliationNotice ledger={ledgerData} toDate={params.toDate} />
            </>
          )}

          <DataTable
            columns={columns}
            data={ledgerData?.entries.items ?? []}
            isLoading={ledger.isLoading}
            isError={ledger.isError}
            error={ledger.error}
            onRetry={() => void ledger.refetch()}
            isRefreshing={ledger.isPlaceholderData}
            totalItems={ledgerData?.entries.totalItems ?? 0}
            pagination={pagination}
            onPaginationChange={onPaginationChange}
            hasActiveFilters={params.fromDate !== null || params.toDate !== null}
            onClearFilters={() => setParams({ fromDate: null, toDate: null, page: DEFAULT_PAGE })}
            empty={{
              icon: BookOpen,
              title: "Chưa có biến động trong khoảng thời gian này",
              description: "Đổi khoảng ngày hoặc nhập kho để phát sinh biến động.",
            }}
          />
        </div>
      )}
    </div>
  );
};

export default AdminInventoryLedgerPage;
