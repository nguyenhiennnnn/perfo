import type { ScheduleSuggestionAction, ScheduleSuggestionType } from "../types";

const TIME_SLOT_LABELS: Record<string, string> = {
  Morning: "Sáng (trước 12h)",
  Afternoon: "Chiều (12h–18h)",
  Evening: "Tối (18h–22h)",
  Late: "Khuya (từ 22h)",
};

export const SUGGESTION_TYPE_META: {
  type: ScheduleSuggestionType;
  title: string;
  description: string;
}[] = [
  { type: "HighDemand", title: "Nhu cầu cao", description: "Phim lấp đầy cao, nên tăng suất." },
  { type: "LowDemand", title: "Nhu cầu thấp", description: "Phim lấp đầy thấp, nên giảm hoặc chuyển suất." },
  { type: "EmptySlot", title: "Khung giờ trống", description: "Khung giờ chưa tận dụng, có thể lấp thêm suất." },
];

const ACTION_LABELS: Record<ScheduleSuggestionAction, string> = {
  AddShowtimes: "Thêm suất",
  ReduceShowtimes: "Giảm suất",
  MoveToSmallerRoom: "Chuyển sang phòng nhỏ hơn",
  MoveToBusierSlot: "Chuyển sang khung giờ đông hơn",
  FillEmptySlot: "Lấp khung giờ trống",
};

export const getTimeSlotLabel = (slot: string | null | undefined) =>
  slot ? (TIME_SLOT_LABELS[slot] ?? slot) : "—";

export const getSuggestionActionLabel = (action: ScheduleSuggestionAction) =>
  ACTION_LABELS[action] ?? action;
