import { TriangleAlert } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import {
  Table,
  TableBody,
  TableCell,
  TableFooter,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { cn } from "@/lib/utils";
import { formatCurrency, formatNumber, formatPercent } from "@/lib/formatters";
import type {
  FinanceReportBlockDto,
  FinanceReportCell,
  FinanceReportColumnDto,
} from "../types";

const isNumeric = (type: string) => type === "Money" || type === "Number" || type === "Percent";

export const formatFinanceCell = (column: FinanceReportColumnDto, value: FinanceReportCell) => {
  if (value == null || value === "") return "—";
  if (!isNumeric(column.type)) return String(value);
  const n = Number(value);
  if (Number.isNaN(n)) return String(value);
  if (column.type === "Money") return formatCurrency(n);
  if (column.type === "Percent") return formatPercent(n);
  return formatNumber(n);
};

interface FinanceBlockTableProps {
  block: FinanceReportBlockDto;
  className?: string;
}

export const FinanceBlockTable = ({ block, className }: FinanceBlockTableProps) => {
  const totals = block.totals ?? null;
  const hasTotals = totals != null && block.columns.some((c) => totals[c.key] != null);

  return (
    <Card className={className}>
      <CardHeader>
        <CardTitle>{block.title}</CardTitle>
      </CardHeader>
      <CardContent className="space-y-3">
        {block.warnings && block.warnings.length > 0 && (
          <div className="flex items-start gap-2 bg-warning/10 p-3 text-sm text-foreground">
            <TriangleAlert className="mt-0.5 size-4 shrink-0 text-warning" />
            <div className="space-y-1">
              {block.warnings.map((w) => (
                <p key={w}>{w}</p>
              ))}
            </div>
          </div>
        )}

        <Table>
          <TableHeader>
            <TableRow>
              {block.columns.map((column) => (
                <TableHead key={column.key} className={cn(isNumeric(column.type) && "text-right")}>
                  {column.title}
                </TableHead>
              ))}
            </TableRow>
          </TableHeader>
          <TableBody>
            {block.rows.map((row, index) => (
              <TableRow key={index}>
                {block.columns.map((column, colIndex) => (
                  <TableCell
                    key={column.key}
                    className={cn(
                      isNumeric(column.type) ? "text-right tabular-nums" : colIndex === 0 && "font-medium",
                    )}
                  >
                    {formatFinanceCell(column, row[column.key])}
                  </TableCell>
                ))}
              </TableRow>
            ))}
          </TableBody>
          {hasTotals && totals && (
            <TableFooter>
              <TableRow>
                {block.columns.map((column, colIndex) => (
                  <TableCell
                    key={column.key}
                    className={cn("font-semibold", isNumeric(column.type) && "text-right tabular-nums")}
                  >
                    {colIndex === 0 && totals[column.key] == null
                      ? "Tổng"
                      : totals[column.key] == null
                        ? ""
                        : formatFinanceCell(column, totals[column.key])}
                  </TableCell>
                ))}
              </TableRow>
            </TableFooter>
          )}
        </Table>
      </CardContent>
    </Card>
  );
};
