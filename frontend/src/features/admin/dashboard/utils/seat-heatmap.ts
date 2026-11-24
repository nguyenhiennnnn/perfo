import type { CSSProperties } from "react";
import type { SeatHeatmapMode } from "@/hooks/use-query-params";
import type { DashboardSeatHeatmapSeatDto } from "@/features/admin/dashboard/types";

export const SEAT_HEATMAP_MODE_OPTIONS: { value: SeatHeatmapMode; label: string }[] = [
  { value: "sold", label: "Đã bán" },
  { value: "abandoned", label: "Chọn rồi bỏ" },
];

const HEAT_LEVELS = 4;
const LEVEL_PERCENT = [0, 28, 48, 72, 100] as const;

const MODE_TOKEN: Record<SeatHeatmapMode, string> = {
  sold: "--primary",
  abandoned: "--warning",
};

export const getSeatHeatValue = (
  seat: Pick<DashboardSeatHeatmapSeatDto, "timesSold" | "timesAbandoned">,
  mode: SeatHeatmapMode,
) => (mode === "sold" ? seat.timesSold : seat.timesAbandoned);

export const getHeatLevel = (value: number, max: number) =>
  value <= 0 || max <= 0 ? 0 : Math.min(HEAT_LEVELS, Math.ceil((value / max) * HEAT_LEVELS));

export const getHeatSeatStyle = (level: number, mode: SeatHeatmapMode): CSSProperties => {
  if (level === 0) return {};
  const bg = `color-mix(in oklch, var(${MODE_TOKEN[mode]}) ${LEVEL_PERCENT[level]}%, var(--seat-base))`;
  return {
    "--seat-bg": bg,
    "--seat-head": `color-mix(in oklch, var(--foreground) 24%, ${bg})`,
    color: mode === "sold" && level >= 3 ? "var(--primary-foreground)" : undefined,
  } as CSSProperties;
};

export interface HeatLegendStep {
  level: number;
  label: string;
}

export const buildHeatLegend = (max: number): HeatLegendStep[] => {
  const steps: HeatLegendStep[] = [{ level: 0, label: "0" }];
  for (let level = 1; level <= HEAT_LEVELS; level++) {
    const lo = Math.floor((max * (level - 1)) / HEAT_LEVELS) + 1;
    const hi = Math.floor((max * level) / HEAT_LEVELS);
    if (lo > hi) continue;
    steps.push({ level, label: lo === hi ? `${lo}` : `${lo}–${hi}` });
  }
  return steps;
};

export const getHeatSwatchStyle = (level: number, mode: SeatHeatmapMode): CSSProperties => ({
  backgroundColor:
    level === 0
      ? "color-mix(in oklch, var(--secondary) 86%, var(--foreground))"
      : `color-mix(in oklch, var(${MODE_TOKEN[mode]}) ${LEVEL_PERCENT[level]}%, color-mix(in oklch, var(--secondary) 86%, var(--foreground)))`,
});
