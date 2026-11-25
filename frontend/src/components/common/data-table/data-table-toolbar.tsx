import type { ReactNode } from "react";
import type { ReactTable, RowData } from "@tanstack/react-table";

import { DataTableViewOptions } from "@/components/common/data-table/data-table-view-options";
import type { DataTableFeatures } from "@/components/common/data-table/data-table-features";
import { FilterBar, FilterSearch } from "@/components/common/filter/filter-bar";
import { SEARCH_PLACEHOLDER } from "@/config/default-constants";

interface DataTableToolbarProps<TData extends RowData> {
  table: ReactTable<DataTableFeatures, TData>;
  searchValue: string;
  onSearchChange?: (value: string) => void;
  searchPlaceholder?: string;
  filters?: ReactNode;
  actions?: ReactNode;
}

export const DataTableToolbar = <TData extends RowData>({
  table,
  searchValue,
  onSearchChange,
  searchPlaceholder = SEARCH_PLACEHOLDER,
  filters,
  actions,
}: DataTableToolbarProps<TData>) => {
  return (
    <div className="flex flex-wrap items-center justify-between gap-2">
      <FilterBar className="min-w-0 flex-1">
        {onSearchChange && (
          <FilterSearch
            value={searchValue}
            onChange={onSearchChange}
            placeholder={searchPlaceholder}
          />
        )}
        {filters}
      </FilterBar>
      <div className="flex shrink-0 items-center gap-2">
        {actions}
        <DataTableViewOptions table={table} />
      </div>
    </div>
  );
};
