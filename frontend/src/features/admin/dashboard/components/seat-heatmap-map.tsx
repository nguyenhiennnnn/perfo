import { useMemo, useState } from "react";
import { cn } from "@/lib/utils";
import { formatNumber } from "@/lib/formatters";
import type { SeatHeatmapMode } from "@/hooks/use-query-params";
import { SeatGrid } from "@/components/common/seat-map/seat-grid";
import { SeatMapFrame } from "@/components/common/seat-map/seat-map-frame";
import { seatTypeLabel } from "@/components/common/seat-map/seat-kind";
import { useSeatGridModel } from "@/components/common/seat-map/use-seat-grid-model";
import {
  deriveGridDimensions,
  generateRowLabel,
  seatCellKey,
  type SeatCellData,
} from "@/components/common/seat-map/seat-map-types";
import type {
  DashboardSeatHeatmapDto,
  DashboardSeatHeatmapSeatDto,
} from "@/features/admin/dashboard/types";
import {
  buildHeatLegend,
  getHeatLevel,
  getHeatSeatStyle,
  getHeatSwatchStyle,
  getSeatHeatValue,
} from "../utils/seat-heatmap";

const INACTIVE_LEGEND = [{ label: "Ghế đã tắt", state: "inactive" as const, group: "status" }];

interface SeatHeatmapMapProps {
  data: DashboardSeatHeatmapDto;
  fallbackRows: number;
  fallbackCols: number;
  mode: SeatHeatmapMode;
}

const describeSeat = (code: string, seat: DashboardSeatHeatmapSeatDto, sold: number, abandoned: number) =>
  `Ghế ${code} · ${seatTypeLabel(seat.seatTypeName, seat.isCouple)}${seat.isActive ? "" : " · đã tắt"} · đã bán ${formatNumber(sold)} lần · chọn rồi bỏ ${formatNumber(abandoned)} lần`;

export const SeatHeatmapMap = ({ data, fallbackRows, fallbackCols, mode }: SeatHeatmapMapProps) => {
  const [activeKey, setActiveKey] = useState<string | null>(null);

  const { rows, cols } = deriveGridDimensions(data.seats, fallbackRows, fallbackCols);

  const seatByKey = useMemo(
    () => new Map(data.seats.map((s) => [seatCellKey(s.seatRow, s.seatNumber), s])),
    [data.seats],
  );

  const cells = useMemo(() => {
    const list: SeatCellData[] = data.seats.map((seat) => ({
      seatRow: seat.seatRow,
      seatNumber: seat.seatNumber,
      seatId: seat.seatId,
      seatTypeId: seat.seatTypeId,
      seatTypeName: seat.seatTypeName,
      isCouple: seat.isCouple,
      coupleId: seat.coupleId,
      isActive: seat.isActive,
      isAisle: false,
    }));
    const usedCols = new Set(data.seats.map((s) => s.seatNumber));
    for (let col = 1; col <= cols; col++) {
      if (usedCols.has(col)) continue;
      for (let r = 0; r < rows; r++) {
        list.push({ seatRow: generateRowLabel(r), seatNumber: col, isActive: false, isAisle: true });
      }
    }
    return list;
  }, [data.seats, rows, cols]);

  const { rowLabels, cellAt } = useSeatGridModel({ rowsCount: rows, cols, cells });

  const max = mode === "sold" ? data.maxTimesSold : data.maxTimesAbandoned;
  const legend = buildHeatLegend(max);
  const hasInactive = data.seats.some((s) => !s.isActive);
  const activeSeat = activeKey ? seatByKey.get(activeKey) : undefined;

  return (
    <SeatMapFrame legend={hasInactive ? INACTIVE_LEGEND : []}>
      <div className="mb-4 flex flex-wrap items-center justify-center gap-x-4 gap-y-2">
        <span className="text-sm text-muted-foreground">
          {mode === "sold" ? "Số lần bán" : "Số lần chọn rồi bỏ"}
        </span>
        {legend.map((step) => (
          <span key={step.level} className="flex items-center gap-1.5 text-sm text-foreground/90">
            <span
              className="size-4 shrink-0 border border-border"
              style={getHeatSwatchStyle(step.level, mode)}
              aria-hidden
            />
            <span className="t-code tabular-nums">{step.label}</span>
          </span>
        ))}
      </div>

      <SeatGrid
        rowLabels={rowLabels}
        cols={cols}
        cellAt={cellAt}
        fitWidth
        compactAisles
        renderCell={(cell, _rowLabel, col) => {
          if (!cell || cell.isAisle) return null;
          const key = seatCellKey(cell.seatRow, cell.seatNumber);
          const seat = seatByKey.get(key);
          if (!seat) return null;

          const isCouple = cell.coupleSpan === 2;
          const partner = isCouple
            ? seatByKey.get(seatCellKey(cell.seatRow, cell.seatNumber + 1))
            : undefined;
          const sold = Math.max(seat.timesSold, partner?.timesSold ?? 0);
          const abandoned = Math.max(seat.timesAbandoned, partner?.timesAbandoned ?? 0);
          const code = isCouple
            ? `${cell.seatRow}${cell.seatNumber}-${cell.seatRow}${cell.seatNumber + 1}`
            : `${cell.seatRow}${cell.seatNumber}`;
          const label = isCouple ? `${col}-${col + 1}` : `${col}`;
          const level = seat.isActive
            ? getHeatLevel(mode === "sold" ? sold : abandoned, max)
            : 0;
          const text = describeSeat(code, seat, sold, abandoned);
          const isActiveCell = activeKey === key;

          return (
            <button
              type="button"
              title={text}
              aria-label={text}
              aria-pressed={isActiveCell}
              data-state={seat.isActive ? "available" : "inactive"}
              onClick={() => setActiveKey(isActiveCell ? null : key)}
              className={cn("seat seat--fill select-none", seat.isActive && "cursor-pointer")}
              style={{
                ...(seat.isActive ? getHeatSeatStyle(level, mode) : {}),
                ...(isActiveCell ? { boxShadow: "inset 0 0 0 2px var(--foreground)" } : {}),
              }}
            >
              <span className="seat__head" aria-hidden />
              <span className="seat__label">{label}</span>
            </button>
          );
        }}
      />

      <p className="mt-4 min-h-5 text-center text-sm text-muted-foreground">
        {activeSeat
          ? describeSeat(
              `${activeSeat.seatRow}${activeSeat.seatNumber}`,
              activeSeat,
              activeSeat.timesSold,
              activeSeat.timesAbandoned,
            )
          : "Chọn một ghế để xem chi tiết."}
      </p>
    </SeatMapFrame>
  );
};
