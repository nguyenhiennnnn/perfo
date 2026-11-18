// Hằng số hub, tên sự kiện và kiểu payload SignalR dùng chung (không chứa logic kết nối).
import { ENV } from "@/config/env";
import { API_VERSION_PREFIX, SIGNALR_HUB_PATH } from "@/api/endpoints";

export const SIGNALR_HUB_URL = `${ENV.API_BASE_URL.replace(API_VERSION_PREFIX, "")}${SIGNALR_HUB_PATH}`;

export const SignalREvents = {
  SeatHeld: "seat.held",
  SeatBooked: "seat.booked",
  SeatReleased: "seat.released",
  HoldExpired: "hold.expired",
  ImportExportJobProgress: "import-export-job.progress",
  ImportExportJobCompleted: "import-export-job.completed",
  ImportExportJobFailed: "import-export-job.failed",
} as const;

export type SignalREventName =
  (typeof SignalREvents)[keyof typeof SignalREvents];

export interface SeatHeldPayload {
  showtimeId: number;
  seatIds: number[];
  heldByUserId: number;
  heldUntil: string;
}

export interface SeatBookedPayload {
  showtimeId: number;
  seatIds: number[];
}

export interface SeatReleasedPayload {
  showtimeId: number;
  seatIds: number[];
}

export interface HoldExpiredPayload {
  showtimeId: number;
  bookingId: number;
  seatIds: number[];
}
