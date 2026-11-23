import { useMemo, useState } from "react";
import { Calculator } from "lucide-react";

import { PageHeader } from "@/components/common/page-header";
import { EmptyState } from "@/components/common/empty-state";
import { QueryState } from "@/components/common/query-state";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { DatePickerField } from "@/components/common/date-time/date-picker-field";
import { DateTimePickerField } from "@/components/common/date-time/datetime-picker-field";
import { SingleSelectField } from "@/components/common/form/single-select-field";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Field, FieldDescription, FieldLabel } from "@/components/ui/field";
import { getBusinessToday } from "@/lib/business-time";
import { formatCurrency, formatDateTime, formatPercent, formatTime } from "@/lib/formatters";
import { useAdminPricePreviewParams } from "@/hooks/use-query-params";
import { useAdminShowtimes } from "@/features/admin/showtimes/hooks/use-admin-showtimes";
import { LOOKUP_PAGE_SIZE } from "@/config/default-constants";

import { useAdminPricePreview } from "../hooks/use-admin-price-preview";
import { PriceBreakdown, PriceTable } from "../components/price-table";
import {
  formatRuleAdjustment,
  getPricingModeLabel,
  getRuleTypeLabel,
} from "../utils/pricing-labels";

const AdminPricePreviewPage = () => {
  const [params, setParams] = useAdminPricePreviewParams();
  const [showtimeSearch, setShowtimeSearch] = useState("");
  const [selectedCell, setSelectedCell] = useState<{
    seatTypeIndex: number;
    ticketTypeId: number;
  } | null>(null);

  const date = params.date ?? getBusinessToday();

  const showtimes = useAdminShowtimes({ date, pageSize: LOOKUP_PAGE_SIZE });
  const preview = useAdminPricePreview(params.showtimeId, params.at ?? undefined);

  const showtimeOptions = useMemo(
    () => showtimes.data?.items ?? [],
    [showtimes.data],
  );

  const selectedCellData = useMemo(() => {
    if (!selectedCell || !preview.data) return null;
    const row = preview.data.rows[selectedCell.seatTypeIndex];
    const ticketType = preview.data.ticketTypes.find(
      (t) => t.ticketTypeId === selectedCell.ticketTypeId,
    );
    const quote = row?.prices.find((p) => p.ticketTypeId === selectedCell.ticketTypeId);
    if (!row || !ticketType || !quote) return null;
    return { seatTypeName: row.name, ticketTypeName: ticketType.name, quote };
  }, [selectedCell, preview.data]);

  const handleDateChange = (value: string | null) => {
    setParams({ date: value, showtimeId: null });
    setSelectedCell(null);
  };

  const handleShowtimeChange = (value: string | number | null) => {
    setParams({ showtimeId: value === null ? null : Number(value) });
    setSelectedCell(null);
  };

  return (
    <div className="space-y-6">
      <PageHeader
        title="Xem trước giá"
        description="Chọn một suất chiếu để xem bảng giá từng loại ghế và loại vé cùng các quy tắc đang tác động."
      />

      <Card>
        <CardContent className="grid grid-cols-1 gap-4 md:grid-cols-3">
          <Field>
            <FieldLabel>Ngày chiếu</FieldLabel>
            <DatePickerField value={date} onChange={handleDateChange} clearable={false} />
          </Field>

          <Field>
            <FieldLabel>Suất chiếu</FieldLabel>
            <SingleSelectField
              value={params.showtimeId}
              onChange={handleShowtimeChange}
              options={showtimeOptions}
              getOptionValue={(o) => o.showtimeId}
              getOptionLabel={(o) => `${formatTime(o.startTime)} · ${o.movieTitle} · ${o.roomName}`}
              selectedLabel={
                preview.data
                  ? `${formatTime(preview.data.startTime)} · ${preview.data.movieTitle} · ${preview.data.roomName}`
                  : undefined
              }
              searchValue={showtimeSearch}
              onSearchChange={setShowtimeSearch}
              isLoading={showtimes.isLoading}
              placeholder="Chọn suất chiếu"
              emptyText="Không có suất chiếu trong ngày này."
              clearable={false}
            />
          </Field>

          <Field>
            <FieldLabel>Thời điểm tính giá</FieldLabel>
            <DateTimePickerField
              value={params.at}
              onChange={(value) => {
                setParams({ at: value });
                setSelectedCell(null);
              }}
              placeholder="Hiện tại"
            />
            <FieldDescription>
              Để trống để tính theo thời điểm hiện tại. Chọn thời điểm khác để xem mức lấp đầy và giá tại lúc đó.
            </FieldDescription>
          </Field>
        </CardContent>
      </Card>

      {params.showtimeId === null ? (
        <EmptyState
          icon={Calculator}
          title="Chưa chọn suất chiếu"
          description="Chọn ngày và suất chiếu để xem bảng giá."
        />
      ) : (
        <QueryState
          query={preview}
          skeleton={
            <div className="space-y-4">
              <CardSkeleton className="h-32" />
              <CardSkeleton className="h-64" />
            </div>
          }
          errorTitle="Không tải được bảng giá"
        >
          {(table) => (
            <div className="space-y-6">
              <Card>
                <CardHeader>
                  <CardTitle className="flex flex-wrap items-center gap-2">
                    <span>{table.movieTitle}</span>
                    <Badge variant="secondary">{getPricingModeLabel(table.pricingMode)}</Badge>
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  <dl className="grid grid-cols-2 gap-4 text-sm md:grid-cols-4">
                    <div>
                      <dt className="text-muted-foreground">Phòng chiếu</dt>
                      <dd className="font-medium">{table.roomName}</dd>
                    </div>
                    <div>
                      <dt className="text-muted-foreground">Giờ chiếu</dt>
                      <dd className="font-medium">{formatDateTime(table.startTime)}</dd>
                    </div>
                    <div>
                      <dt className="text-muted-foreground">Giá cơ sở</dt>
                      <dd className="font-medium tabular-nums">{formatCurrency(table.basePrice)}</dd>
                    </div>
                    <div>
                      <dt className="text-muted-foreground">Thời điểm tính</dt>
                      <dd className="font-medium">{formatDateTime(table.at)}</dd>
                    </div>
                    <div>
                      <dt className="text-muted-foreground">Mức lấp đầy</dt>
                      <dd className="font-medium tabular-nums">
                        {formatPercent(table.occupancy.percent)}
                      </dd>
                    </div>
                    <div>
                      <dt className="text-muted-foreground">Đã bán / đang giữ / tổng ghế</dt>
                      <dd className="font-medium tabular-nums">
                        {table.occupancy.bookedSeats} / {table.occupancy.heldSeats} /{" "}
                        {table.occupancy.totalSeats}
                      </dd>
                    </div>
                  </dl>
                </CardContent>
              </Card>

              <Card>
                <CardHeader>
                  <CardTitle>Quy tắc đang tác động</CardTitle>
                </CardHeader>
                <CardContent>
                  {table.appliedRules.length === 0 ? (
                    <p className="text-sm text-muted-foreground">
                      Không có quy tắc giá nào áp dụng cho suất này tại thời điểm tính.
                    </p>
                  ) : (
                    <ul className="divide-y divide-border">
                      {table.appliedRules.map((rule) => (
                        <li
                          key={rule.pricingRuleId}
                          className="flex flex-col gap-1 py-2 sm:flex-row sm:items-center sm:justify-between"
                        >
                          <div className="flex flex-col gap-1">
                            <div className="flex flex-wrap items-center gap-1.5">
                              <span className="text-sm font-medium">{rule.name}</span>
                              <Badge variant="outline">{getRuleTypeLabel(rule.ruleType)}</Badge>
                              <span className="text-xs text-muted-foreground">
                                Ưu tiên {rule.priority}
                              </span>
                            </div>
                            <span className="text-sm text-muted-foreground">{rule.reason}</span>
                          </div>
                          <span className="text-sm font-semibold tabular-nums">
                            {formatRuleAdjustment(rule.adjustmentMode, rule.value)}
                          </span>
                        </li>
                      ))}
                    </ul>
                  )}
                </CardContent>
              </Card>

              <PriceTable table={table} selected={selectedCell} onSelect={setSelectedCell} />

              {selectedCellData && <PriceBreakdown cell={selectedCellData} />}
            </div>
          )}
        </QueryState>
      )}
    </div>
  );
};

export default AdminPricePreviewPage;
