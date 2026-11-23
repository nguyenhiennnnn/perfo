import { Ban, ShieldCheck } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { cn } from "@/lib/utils";
import { formatCurrency } from "@/lib/formatters";

import type { PriceQuoteDto, ShowtimePriceTableDto } from "../types";
import {
  formatMultiplier,
  formatSignedCurrency,
  getAdjustmentKindLabel,
  getRuleTypeLabel,
} from "../utils/pricing-labels";

export interface SelectedPriceCell {
  seatTypeName: string;
  ticketTypeName: string;
  quote: PriceQuoteDto;
}

interface PriceTableProps {
  table: ShowtimePriceTableDto;
  selected: { seatTypeIndex: number; ticketTypeId: number } | null;
  onSelect: (cell: { seatTypeIndex: number; ticketTypeId: number }) => void;
}

export const PriceTable = ({ table, selected, onSelect }: PriceTableProps) => (
  <Card>
    <CardHeader>
      <CardTitle>Bảng giá theo loại ghế và loại vé</CardTitle>
    </CardHeader>
    <CardContent>
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Loại ghế</TableHead>
            {table.ticketTypes.map((ticketType) => (
              <TableHead key={ticketType.ticketTypeId} className="text-right">
                <div className="flex flex-col items-end">
                  <span>{ticketType.name}</span>
                  <span className="text-xs font-normal text-muted-foreground">
                    {formatMultiplier(ticketType.priceMultiplier)}
                  </span>
                </div>
              </TableHead>
            ))}
          </TableRow>
        </TableHeader>
        <TableBody>
          {table.rows.map((row, rowIndex) => (
            <TableRow key={row.seatTypeId ?? `row-${rowIndex}`}>
              <TableCell>
                <div className="flex flex-col">
                  <span className="font-medium">{row.name}</span>
                  <span className="text-xs text-muted-foreground">
                    {formatMultiplier(row.priceMultiplier)}
                  </span>
                </div>
              </TableCell>
              {table.ticketTypes.map((ticketType) => {
                const quote = row.prices.find((p) => p.ticketTypeId === ticketType.ticketTypeId);
                if (!quote) return <TableCell key={ticketType.ticketTypeId} />;

                const isSelected =
                  selected?.seatTypeIndex === rowIndex &&
                  selected.ticketTypeId === ticketType.ticketTypeId;
                const delta = quote.listPrice - quote.referencePrice;

                return (
                  <TableCell key={ticketType.ticketTypeId} className="p-1 text-right">
                    <button
                      type="button"
                      onClick={() =>
                        onSelect({ seatTypeIndex: rowIndex, ticketTypeId: ticketType.ticketTypeId })
                      }
                      aria-pressed={isSelected}
                      className={cn(
                        "w-full cut-xs px-2 py-1.5 text-right transition-colors hover:bg-muted focus-visible:outline-2 focus-visible:outline-ring",
                        isSelected && "bg-muted ring-1 ring-primary",
                      )}
                    >
                      {quote.isEligible ? (
                        <div className="flex flex-col items-end">
                          <span className="font-semibold tabular-nums text-primary-text">
                            {formatCurrency(quote.listPrice)}
                          </span>
                          {delta !== 0 && (
                            <span
                              className={cn(
                                "text-xs tabular-nums",
                                delta > 0 ? "text-warning-text" : "text-success",
                              )}
                            >
                              {formatSignedCurrency(delta)}
                            </span>
                          )}
                        </div>
                      ) : (
                        <span className="inline-flex items-center gap-1 text-xs text-muted-foreground">
                          <Ban className="size-3" />
                          Không áp dụng
                        </span>
                      )}
                    </button>
                  </TableCell>
                );
              })}
            </TableRow>
          ))}
        </TableBody>
      </Table>
      <p className="mt-3 text-xs text-muted-foreground">
        Dòng nhỏ bên dưới giá là phần chênh lệch do quy tắc giá động so với giá gốc theo loại ghế và loại vé. Chọn một ô để xem chi tiết.
      </p>
    </CardContent>
  </Card>
);

interface PriceBreakdownProps {
  cell: SelectedPriceCell;
}

export const PriceBreakdown = ({ cell }: PriceBreakdownProps) => {
  const { quote } = cell;

  return (
    <Card>
      <CardHeader>
        <CardTitle>
          Chi tiết giá: {cell.seatTypeName} · {cell.ticketTypeName}
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        {!quote.isEligible && (
          <div className="flex items-start gap-2 text-sm text-muted-foreground">
            <Ban className="mt-0.5 size-4 shrink-0" />
            <span>{quote.ineligibleReason ?? "Loại vé này không áp dụng cho suất chiếu."}</span>
          </div>
        )}

        <dl className="grid grid-cols-1 gap-x-6 gap-y-2 text-sm sm:grid-cols-2">
          <div className="flex justify-between gap-4">
            <dt className="text-muted-foreground">Giá cơ sở của suất</dt>
            <dd className="tabular-nums">{formatCurrency(quote.basePrice)}</dd>
          </div>
          <div className="flex justify-between gap-4">
            <dt className="text-muted-foreground">Hệ số loại ghế</dt>
            <dd className="tabular-nums">{formatMultiplier(quote.seatMultiplier)}</dd>
          </div>
          <div className="flex justify-between gap-4">
            <dt className="text-muted-foreground">Hệ số loại vé</dt>
            <dd className="tabular-nums">{formatMultiplier(quote.ticketTypeMultiplier)}</dd>
          </div>
          <div className="flex justify-between gap-4">
            <dt className="text-muted-foreground">Giá tham chiếu</dt>
            <dd className="tabular-nums">{formatCurrency(quote.referencePrice)}</dd>
          </div>
          <div className="flex justify-between gap-4">
            <dt className="text-muted-foreground">Điều chỉnh giá động</dt>
            <dd className="tabular-nums">{formatSignedCurrency(quote.dynamicAdjustment)}</dd>
          </div>
          <div className="flex justify-between gap-4 font-semibold">
            <dt>Giá niêm yết</dt>
            <dd className="tabular-nums text-primary-text">{formatCurrency(quote.listPrice)}</dd>
          </div>
        </dl>

        {quote.adjustments.length > 0 ? (
          <ul className="divide-y divide-border border-t border-border">
            {quote.adjustments.map((adjustment, index) => (
              <li
                key={`${adjustment.kind}-${adjustment.pricingRuleId ?? index}`}
                className="flex flex-col gap-1 py-2 sm:flex-row sm:items-center sm:justify-between"
              >
                <div className="flex flex-col gap-1">
                  <div className="flex flex-wrap items-center gap-1.5">
                    <Badge variant={adjustment.kind === "Rule" ? "secondary" : "warning"}>
                      {getAdjustmentKindLabel(adjustment.kind)}
                    </Badge>
                    {adjustment.ruleName && (
                      <span className="text-sm font-medium">{adjustment.ruleName}</span>
                    )}
                    {adjustment.ruleType && (
                      <span className="text-xs text-muted-foreground">
                        {getRuleTypeLabel(adjustment.ruleType)}
                      </span>
                    )}
                  </div>
                  <span className="text-sm text-muted-foreground">{adjustment.reason}</span>
                </div>
                <span className="text-sm font-medium tabular-nums">
                  {formatSignedCurrency(adjustment.amount)}
                </span>
              </li>
            ))}
          </ul>
        ) : (
          <p className="flex items-center gap-2 border-t border-border pt-3 text-sm text-muted-foreground">
            <ShieldCheck className="size-4" />
            Không có quy tắc nào tác động lên ô giá này.
          </p>
        )}
      </CardContent>
    </Card>
  );
};
