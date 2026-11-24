import type { AutoScheduleRequest, AutoScheduleResponseDto } from "@/features/admin/showtimes/types";

export interface ScheduleReportParams {
  from: string;
  to: string;
}

export interface ScheduleReportRowDto {
  movieId: number;
  movieTitle: string | null;
  roomId: number;
  roomName: string | null;
  timeSlot: string;
  showtimeCount: number;
  sellableSeats: number;
  ticketsSold: number;
  occupancyPercent: number | null;
  netRevenue: number;
  revenuePerShowtime: number;
  revenuePerSeatHour: number | null;
}

export interface ScheduleReportDto {
  from: string;
  to: string;
  items: ScheduleReportRowDto[];
}

export type ScheduleSuggestionType = "HighDemand" | "LowDemand" | "EmptySlot";

export type ScheduleSuggestionAction =
  | "AddShowtimes"
  | "ReduceShowtimes"
  | "MoveToSmallerRoom"
  | "MoveToBusierSlot"
  | "FillEmptySlot";

export interface ScheduleSuggestionDto {
  id: string;
  type: ScheduleSuggestionType;
  action: ScheduleSuggestionAction;
  movieId: number;
  movieTitle: string | null;
  roomId: number | null;
  roomName: string | null;
  timeSlot: string | null;
  fromRoomId: number | null;
  fromRoomName: string | null;
  showtimeCount: number;
  occupancyPercent: number;
  targetShowtimeCount: number | null;
  targetOccupancyPercent: number | null;
  reason: string;
}

export interface ScheduleSuggestionsDto {
  from: string;
  to: string;
  highOccupancyPercent: number;
  lowOccupancyPercent: number;
  minShowtimes: number;
  items: ScheduleSuggestionDto[];
}

export interface ApplyScheduleSuggestionsRequest {
  suggestionIds: string[];
}

export interface ScheduleSuggestionNotApplicableDto {
  id: string;
  reason: string;
}

export interface ApplyScheduleSuggestionsResponseDto {
  request: AutoScheduleRequest | null;
  preview: AutoScheduleResponseDto | null;
  appliedIds: string[];
  notApplicable: ScheduleSuggestionNotApplicableDto[];
}
