import type { ReactNode } from "react";
import {
  useTable,
  type ColumnDef,
  type ColumnFiltersState,
  type OnChangeFn,
  type PaginationState,
  type RowData,
  type RowSelectionState,
  type SortingState,
} from "@tanstack/react-table";

import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Inbox, type LucideIcon } from "lucide-react";
import { EmptyState } from "@/components/common/empty-state";
import { ErrorState } from "@/components/common/error-state";
import { TableRowsSkeleton } from "@/components/common/skeleton-shapes";
import { STATE_MESSAGES } from "@/config/error-messages";
import { cn } from "@/lib/utils";
import {
  features,
  type DataTableFeatures,
} from "@/components/common/data-table/data-table-features";
import { DataTablePagination } from "@/components/common/data-table/data-table-pagination";
import { DataTableToolbar } from "@/components/common/data-table/data-table-toolbar";

export interface DataTableEmpty {
  icon?: LucideIcon;
  title: string;
  description?: string;
  /** Nút tạo mới hoặc hành động tiếp theo khi bảng chưa có dữ liệu. */
  actionLabel?: string;
  onAction?: () => void;
}

interface DataTableProps<TData extends RowData> {
  columns: ColumnDef<DataTableFeatures, TData>[];
  data: TData[];
  isLoading?: boolean;
  isRefreshing?: boolean;
  isError?: boolean;
  error?: unknown;
  /** Gọi refetch của truy vấn; hiện ở nút "Thử lại" khi tải lỗi. */
  onRetry?: () => void;
  totalItems: number;
  pagination: PaginationState;
  onPaginationChange: OnChangeFn<PaginationState>;
  sorting?: SortingState;
  onSortingChange?: OnChangeFn<SortingState>;
  columnFilters?: ColumnFiltersState;
  onColumnFiltersChange?: OnChangeFn<ColumnFiltersState>;
  /** Trạng thái "chưa có dữ liệu". Khi đang tìm hoặc lọc, bảng tự hiện "không có kết quả". */
  empty?: DataTableEmpty;
  hasActiveFilters?: boolean;
  onClearFilters?: () => void;
  searchValue?: string;
  onSearchChange?: (value: string) => void;
  searchPlaceholder?: string;
  toolbarFilters?: ReactNode;
  rowSelection?: RowSelectionState;
  onRowSelectionChange?: OnChangeFn<RowSelectionState>;
  getRowId?: (row: TData) => string;
  bulkActions?: (selectedIds: string[]) => ReactNode;
}

export const DataTable = <TData extends RowData>({
  columns,
  data,
  isLoading,
  isRefreshing,
  isError,
  error,
  onRetry,
  totalItems,
  pagination,
  onPaginationChange,
  sorting,
  onSortingChange,
  columnFilters,
  onColumnFiltersChange,
  empty,
  hasActiveFilters,
  onClearFilters,
  searchValue,
  onSearchChange,
  searchPlaceholder,
  toolbarFilters,
  rowSelection,
  onRowSelectionChange,
  getRowId,
  bulkActions,
}: DataTableProps<TData>) => {
  const handlePaginationChange: OnChangeFn<PaginationState> = (updater) => {
    onRowSelectionChange?.({});
    onPaginationChange(updater);
  };

  const table = useTable({
    features,
    data,
    columns,
    manualPagination: true,
    manualSorting: Boolean(onSortingChange),
    manualFiltering: Boolean(onColumnFiltersChange),
    pageCount: Math.max(Math.ceil(totalItems / pagination.pageSize), 1),
    rowCount: totalItems,
    onPaginationChange: handlePaginationChange,
    onSortingChange,
    onColumnFiltersChange,
    ...(onRowSelectionChange ? { onRowSelectionChange } : {}),
    ...(getRowId ? { getRowId } : {}),
    state: {
      pagination,
      ...(sorting ? { sorting } : {}),
      ...(columnFilters ? { columnFilters } : {}),
      ...(rowSelection !== undefined ? { rowSelection } : {}),
    },
  });

  const hasFilters = hasActiveFilters ?? (Boolean(searchValue) || (columnFilters?.length ?? 0) > 0);
  const clearFilters = onClearFilters ?? (onSearchChange ? () => onSearchChange("") : undefined);
  const rows = table.getRowModel().rows;
  const showError = Boolean(isError) && !isLoading && rows.length === 0;

  const renderMessageRow = (content: ReactNode) => (
    <TableRow>
      <TableCell colSpan={columns.length} className="p-0">
        {content}
      </TableCell>
    </TableRow>
  );

  return (
    <div className="space-y-4">
      {(onSearchChange || bulkActions) && (
        <DataTableToolbar
          table={table}
          searchValue={searchValue ?? ""}
          onSearchChange={onSearchChange ?? (() => { })}
          searchPlaceholder={searchPlaceholder}
          filters={toolbarFilters}
          bulkActions={
            bulkActions && table.getSelectedRowModel().rows.length > 0
              ? bulkActions(table.getSelectedRowIds())
              : undefined
          }
        />
      )}

      <div className="overflow-hidden border">
        <Table>
          <TableHeader>
            {table.getHeaderGroups().map((headerGroup) => (
              <TableRow key={headerGroup.id}>
                {headerGroup.headers.map((header) => (
                  <TableHead key={header.id}>
                    {header.isPlaceholder ? null : (
                      <table.FlexRender header={header} />
                    )}
                  </TableHead>
                ))}
              </TableRow>
            ))}
          </TableHeader>
          <TableBody
            aria-busy={isRefreshing || undefined}
            className={cn(isRefreshing && "opacity-60 transition-opacity")}
          >
            {isLoading ? (
              <TableRowsSkeleton rows={pagination.pageSize} columns={columns.length} />
            ) : showError ? (
              renderMessageRow(
                <ErrorState
                  error={error}
                  onRetry={onRetry ?? (() => window.location.reload())}
                  className="bg-transparent py-12"
                />,
              )
            ) : rows.length ? (
              rows.map((row) => (
                <TableRow
                  key={row.id}
                  data-state={row.getIsSelected() && "selected"}
                >
                  {row.getVisibleCells().map((cell) => (
                    <TableCell key={cell.id}>
                      <table.FlexRender cell={cell} />
                    </TableCell>
                  ))}
                </TableRow>
              ))
            ) : hasFilters ? (
              renderMessageRow(
                <EmptyState variant="filtered" onClearFilters={clearFilters} className="bg-transparent py-12" />,
              )
            ) : (
              renderMessageRow(
                <EmptyState
                  icon={empty?.icon ?? Inbox}
                  title={empty?.title ?? STATE_MESSAGES.empty.title}
                  description={empty?.description}
                  actionLabel={empty?.actionLabel}
                  onAction={empty?.onAction}
                  className="bg-transparent py-12"
                />,
              )
            )}
          </TableBody>
        </Table>
      </div>

      <DataTablePagination table={table} totalItems={totalItems} />
    </div>
  );
};
