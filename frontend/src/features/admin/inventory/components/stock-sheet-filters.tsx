import { DateFilterPicker } from "@/components/common/filter/date-filter-picker";
import { EnumSelectFilter } from "@/components/common/filter/enum-select-filter";
import type { EnumOption } from "@/lib/enum-options";

import { useAdminInventoryItemOptions } from "../hooks/use-admin-inventory";
import { WASTE_REASON_OPTIONS } from "../utils/inventory-labels";

export interface StockSheetFilterValues {
  fromDate: string | null;
  toDate: string | null;
  itemId: number | null;
  reason: string | null;
}

interface StockSheetFiltersProps {
  values: StockSheetFilterValues;
  onChange: (patch: Partial<StockSheetFilterValues>) => void;
  withReason?: boolean;
}

export const StockSheetFilters = ({ values, onChange, withReason = false }: StockSheetFiltersProps) => {
  const items = useAdminInventoryItemOptions();
  const itemOptions: EnumOption[] = (items.data ?? []).map((item) => ({
    value: String(item.inventoryItemId),
    label: item.name,
  }));

  return (
    <>
      <DateFilterPicker
        value={values.fromDate}
        onChange={(fromDate) => onChange({ fromDate })}
        placeholder="Từ ngày"
      />
      <DateFilterPicker
        value={values.toDate}
        onChange={(toDate) => onChange({ toDate })}
        placeholder="Đến ngày"
      />
      <EnumSelectFilter
        value={values.itemId === null ? null : String(values.itemId)}
        onChange={(value) => onChange({ itemId: value === null ? null : Number(value) })}
        options={itemOptions}
        placeholder="Sản phẩm kho"
        allLabel="Tất cả sản phẩm"
      />
      {withReason && (
        <EnumSelectFilter
          value={values.reason}
          onChange={(reason) => onChange({ reason })}
          options={WASTE_REASON_OPTIONS}
          placeholder="Lý do"
          allLabel="Tất cả lý do"
        />
      )}
    </>
  );
};
