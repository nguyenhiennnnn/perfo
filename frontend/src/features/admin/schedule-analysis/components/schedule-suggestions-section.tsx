"use client";

import { useState } from "react";
import { Lightbulb } from "lucide-react";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { toast } from "@/components/ui/toast";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { QueryState } from "@/components/common/query-state";
import { EmptyState } from "@/components/common/empty-state";
import { useDialogClose, useDialogOpen } from "@/stores/dialog-store";
import { formatDate, formatNumber, formatPercent } from "@/lib/formatters";
import { ShowtimeAutoScheduleForm } from "@/features/admin/showtimes/components/forms/showtime-auto-schedule-form";
import { useApplyScheduleSuggestions, useScheduleSuggestions } from "../hooks/use-schedule-analysis";
import type { ScheduleSuggestionDto, ScheduleSuggestionNotApplicableDto } from "../types";
import {
  SUGGESTION_TYPE_META,
  getSuggestionActionLabel,
  getTimeSlotLabel,
} from "../utils/labels";

const SuggestionRow = ({
  item,
  checked,
  disabled,
  onToggle,
}: {
  item: ScheduleSuggestionDto;
  checked: boolean;
  disabled: boolean;
  onToggle: () => void;
}) => {
  const place = [
    item.fromRoomName ? `${item.fromRoomName} →` : null,
    item.roomName,
    item.timeSlot ? getTimeSlotLabel(item.timeSlot) : null,
  ]
    .filter(Boolean)
    .join(" ");

  return (
    <li>
      <label className="flex cursor-pointer items-start gap-3 px-3 py-3">
        <Checkbox
          className="mt-0.5"
          checked={checked}
          disabled={disabled}
          onCheckedChange={onToggle}
        />
        <div className="min-w-0 flex-1 space-y-1">
          <p className="text-sm font-medium text-foreground">
            {item.movieTitle ?? `Phim #${item.movieId}`}
            <span className="ml-2 text-xs font-normal text-muted-foreground">
              {getSuggestionActionLabel(item.action)}
            </span>
          </p>
          {place && <p className="text-xs text-muted-foreground">{place}</p>}
          <p className="text-sm text-muted-foreground">{item.reason}</p>
        </div>
        <div className="shrink-0 text-right text-xs tabular-nums text-muted-foreground">
          <p>
            {formatNumber(item.showtimeCount)} suất
            {item.targetShowtimeCount != null && ` → ${formatNumber(item.targetShowtimeCount)}`}
          </p>
          <p>
            Lấp đầy {formatPercent(item.occupancyPercent)}
            {item.targetOccupancyPercent != null &&
              ` → ${formatPercent(item.targetOccupancyPercent)}`}
          </p>
        </div>
      </label>
    </li>
  );
};

export const ScheduleSuggestionsSection = () => {
  const query = useScheduleSuggestions();
  const apply = useApplyScheduleSuggestions();
  const openDialog = useDialogOpen();
  const closeDialog = useDialogClose();

  const [selected, setSelected] = useState<string[]>([]);
  const [notApplicable, setNotApplicable] = useState<ScheduleSuggestionNotApplicableDto[]>([]);

  const toggle = (id: string) =>
    setSelected((prev) => (prev.includes(id) ? prev.filter((x) => x !== id) : [...prev, id]));

  const toggleGroup = (ids: string[]) =>
    setSelected((prev) =>
      ids.every((id) => prev.includes(id))
        ? prev.filter((id) => !ids.includes(id))
        : [...new Set([...prev, ...ids])],
    );

  const handleApply = () => {
    apply.mutate(
      { suggestionIds: selected },
      {
        onSuccess: (res) => {
          const { preview, request, appliedIds, notApplicable: rejected } = res.data;
          setNotApplicable(rejected);
          setSelected((prev) => prev.filter((id) => !appliedIds.includes(id)));

          if (!preview) {
            toast.add({
              title: "Không có đề xuất nào áp dụng được.",
              description: "Xem lý do ở danh sách bên dưới.",
              type: "error",
            });
            return;
          }

          openDialog({
            title: "Xem trước xếp lịch tự động",
            description: "Lịch được dựng từ các gợi ý đã chọn. Chưa có suất nào được tạo cho đến khi bạn áp dụng.",
            content: (
              <ShowtimeAutoScheduleForm
                onSuccess={closeDialog}
                initialDraft={preview}
                initialRequest={request}
              />
            ),
            size: "xl",
          });
        },
      },
    );
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>Gợi ý tối ưu lịch chiếu</CardTitle>
        <CardDescription>
          Chọn một hoặc nhiều gợi ý rồi áp dụng để xem trước lịch xếp tự động.
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-6">
        <QueryState
          query={query}
          skeleton={<CardSkeleton className="h-64" />}
          isEmpty={(data) => data.items.length === 0}
          empty={
            <EmptyState
              icon={Lightbulb}
              title="Chưa có gợi ý nào"
              description="Hiện chưa có phim, phòng hay khung giờ nào cần điều chỉnh."
            />
          }
          errorTitle="Không tải được gợi ý lịch chiếu"
        >
          {(data) => (
            <div className="space-y-6">
              <p className="text-xs text-muted-foreground">
                Dựa trên dữ liệu {formatDate(data.from)} – {formatDate(data.to)}. Lấp đầy cao từ{" "}
                {data.highOccupancyPercent}%, thấp dưới {data.lowOccupancyPercent}%, tối thiểu{" "}
                {data.minShowtimes} suất.
              </p>

              {SUGGESTION_TYPE_META.map((meta) => {
                const items = data.items.filter((i) => i.type === meta.type);
                if (items.length === 0) return null;
                const ids = items.map((i) => i.id);
                return (
                  <section key={meta.type} className="space-y-2">
                    <div className="flex items-center justify-between gap-3">
                      <div>
                        <h3 className="text-sm font-semibold text-foreground">
                          {meta.title} ({items.length})
                        </h3>
                        <p className="text-xs text-muted-foreground">{meta.description}</p>
                      </div>
                      <Button
                        type="button"
                        variant="outline"
                        size="sm"
                        disabled={apply.isPending}
                        onClick={() => toggleGroup(ids)}
                      >
                        {ids.every((id) => selected.includes(id)) ? "Bỏ chọn nhóm" : "Chọn cả nhóm"}
                      </Button>
                    </div>
                    <ul className="divide-y divide-border border border-border">
                      {items.map((item) => (
                        <SuggestionRow
                          key={item.id}
                          item={item}
                          checked={selected.includes(item.id)}
                          disabled={apply.isPending}
                          onToggle={() => toggle(item.id)}
                        />
                      ))}
                    </ul>
                  </section>
                );
              })}

              <div className="flex items-center justify-between gap-3">
                <p className="text-sm text-muted-foreground">Đã chọn {selected.length} gợi ý</p>
                <Button
                  type="button"
                  onClick={handleApply}
                  disabled={selected.length === 0 || apply.isPending}
                >
                  {apply.isPending ? "Đang xử lý..." : "Áp dụng đề xuất"}
                </Button>
              </div>

              {notApplicable.length > 0 && (
                <div className="space-y-1 bg-destructive/10 p-3 text-sm text-destructive">
                  <p className="font-medium">Không áp dụng được ({notApplicable.length})</p>
                  <ul className="space-y-1">
                    {notApplicable.map((n) => {
                      const source = data.items.find((i) => i.id === n.id);
                      return (
                        <li key={n.id}>
                          {source ? `${source.movieTitle ?? `Phim #${source.movieId}`}: ` : ""}
                          {n.reason}
                        </li>
                      );
                    })}
                  </ul>
                </div>
              )}
            </div>
          )}
        </QueryState>
      </CardContent>
    </Card>
  );
};
