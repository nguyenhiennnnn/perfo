import { ChevronLeft, ChevronRight, RotateCcw } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { DateFilterPicker } from "@/components/common/filter/date-filter-picker";
import type { FinanceReportPeriod } from "@/hooks/use-query-params";
import { FINANCE_PERIOD_OPTIONS } from "../utils/finance-period";

interface FinancePeriodFilterProps {
  period: FinanceReportPeriod;
  anchor: string;
  from: string;
  to: string;
  onPeriodChange: (period: FinanceReportPeriod) => void;
  onAnchorChange: (value: string | null) => void;
  onShift: (direction: 1 | -1) => void;
  onFromChange: (value: string | null) => void;
  onToChange: (value: string | null) => void;
  onReset: () => void;
}

export const FinancePeriodFilter = ({
  period,
  anchor,
  from,
  to,
  onPeriodChange,
  onAnchorChange,
  onShift,
  onFromChange,
  onToChange,
  onReset,
}: FinancePeriodFilterProps) => (
  <div className="flex flex-wrap items-center gap-3 bg-card p-3">
    <Tabs value={period} onValueChange={(value) => onPeriodChange(value as FinanceReportPeriod)}>
      <TabsList>
        {FINANCE_PERIOD_OPTIONS.map((option) => (
          <TabsTrigger key={option.value} value={option.value}>
            {option.label}
          </TabsTrigger>
        ))}
      </TabsList>
    </Tabs>

    {period === "custom" ? (
      <>
        <DateFilterPicker value={from} onChange={onFromChange} placeholder="Từ ngày" />
        <DateFilterPicker value={to} onChange={onToChange} placeholder="Đến ngày" />
      </>
    ) : (
      <div className="flex items-center gap-1">
        <Button variant="outline" size="icon" className="size-10" onClick={() => onShift(-1)} aria-label="Kỳ trước">
          <ChevronLeft className="size-4" />
        </Button>
        <DateFilterPicker value={anchor} onChange={onAnchorChange} placeholder="Chọn ngày" />
        <Button variant="outline" size="icon" className="size-10" onClick={() => onShift(1)} aria-label="Kỳ sau">
          <ChevronRight className="size-4" />
        </Button>
      </div>
    )}

    <Button variant="outline" className="h-10" onClick={onReset}>
      <RotateCcw className="size-4" />
      <span>Đặt lại</span>
    </Button>
  </div>
);
