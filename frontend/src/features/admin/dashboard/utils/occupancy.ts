import { formatNumber } from "@/lib/formatters";
import type { OccupancyGroupBy, ShowtimeKind } from "@/types/enum";
import type { DashboardOccupancyGroupDto } from "@/features/admin/dashboard/types";

export const OCCUPANCY_GROUP_OPTIONS: { value: OccupancyGroupBy; label: string }[] = [
  { value: "Room", label: "Phòng" },
  { value: "TimeSlot", label: "Khung giờ" },
  { value: "Movie", label: "Phim" },
  { value: "Weekday", label: "Thứ" },
];

export const SHOWTIME_KIND_OPTIONS: { value: ShowtimeKind; label: string }[] = [
  { value: "Regular", label: "Suất thường" },
  { value: "Private", label: "Suất riêng" },
  { value: "Event", label: "Sự kiện" },
];

const TIME_SLOT_LABELS: Record<string, string> = {
  Morning: "Sáng (trước 12h)",
  Afternoon: "Chiều (12h–18h)",
  Evening: "Tối (18h–22h)",
  Late: "Khuya (từ 22h)",
};

const WEEKDAY_LABELS: Record<string, string> = {
  Monday: "Thứ hai",
  Tuesday: "Thứ ba",
  Wednesday: "Thứ tư",
  Thursday: "Thứ năm",
  Friday: "Thứ sáu",
  Saturday: "Thứ bảy",
  Sunday: "Chủ nhật",
};

export const getOccupancyGroupLabel = (
  groupBy: OccupancyGroupBy,
  item: Pick<DashboardOccupancyGroupDto, "key" | "name">,
) => {
  if (groupBy === "TimeSlot") return TIME_SLOT_LABELS[item.key] ?? item.key;
  if (groupBy === "Weekday") return WEEKDAY_LABELS[item.key] ?? item.key;
  return item.name ?? item.key;
};

export const formatChangePoints = (points: number | null) =>
  points == null
    ? "—"
    : `${points > 0 ? "+" : ""}${formatNumber(points, { maximumFractionDigits: 1 })} điểm`;

export const getChangeTextClass = (points: number | null) =>
  points == null || points === 0
    ? "text-muted-foreground"
    : points > 0
      ? "text-success"
      : "text-destructive";
