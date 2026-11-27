import { Badge } from "@/components/ui/badge";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { formatNumber, formatPercent } from "@/lib/formatters";
import { cn } from "@/lib/utils";

import type { RetentionCohortDto, RetentionMonthDto } from "../types/retention";
import {
  RETENTION_MONTH_OFFSETS,
  formatCohortMonth,
  getMonthOffsetLabel,
  getRateShade,
} from "../utils/retention-report";

interface RetentionCohortTableProps {
  cohorts: RetentionCohortDto[];
  className?: string;
}

const RetentionCell = ({ month }: { month: RetentionMonthDto | undefined }) => {
  if (!month || month.returnRatePercent === null) {
    return <span className="text-muted-foreground">—</span>;
  }

  const shade = getRateShade(month.returnRatePercent);
  return (
    <div
      className="flex flex-col items-center gap-0.5 rounded-sm px-2 py-1.5"
      style={{ backgroundColor: `color-mix(in oklch, var(--primary) ${shade}%, transparent)` }}
    >
      <span className="font-semibold tabular-nums text-foreground">
        {formatPercent(month.returnRatePercent)}
      </span>
      <span className="text-xs tabular-nums text-muted-foreground">
        {formatNumber(month.returnedCustomers)} khách
      </span>
      {!month.isComplete && (
        <Badge variant="secondary" className="text-[10px]">
          Tạm tính
        </Badge>
      )}
    </div>
  );
};

export const RetentionCohortTable = ({ cohorts, className }: RetentionCohortTableProps) => (
  <div className={cn("rounded-lg border border-border bg-card", className)}>
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Nhóm đăng ký</TableHead>
          <TableHead className="text-right">Số khách</TableHead>
          {RETENTION_MONTH_OFFSETS.map((offset) => (
            <TableHead key={offset} className="text-center">
              {getMonthOffsetLabel(offset)}
            </TableHead>
          ))}
        </TableRow>
      </TableHeader>
      <TableBody>
        {cohorts.map((cohort) => (
          <TableRow key={cohort.cohortMonth}>
            <TableCell className="font-medium tabular-nums text-foreground">
              Tháng {formatCohortMonth(cohort.cohortMonth)}
            </TableCell>
            <TableCell className="text-right tabular-nums">
              {formatNumber(cohort.cohortSize)}
            </TableCell>
            {RETENTION_MONTH_OFFSETS.map((offset) => (
              <TableCell key={offset} className="text-center">
                <RetentionCell month={cohort.months.find((m) => m.monthOffset === offset)} />
              </TableCell>
            ))}
          </TableRow>
        ))}
      </TableBody>
    </Table>
  </div>
);
