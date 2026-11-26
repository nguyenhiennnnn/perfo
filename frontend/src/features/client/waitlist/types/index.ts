import type { WaitlistStatus } from "@/types/enum";

export interface WaitlistEntryDto {
  waitlistEntryId: number;
  showtimeId: number;
  seatCount: number;
  requireAdjacent: boolean;
  status: WaitlistStatus;
  queuePosition?: number | null;
  createdAt: string;
  notifiedAt?: string | null;
  closedAt?: string | null;
  priorityExpiresAt?: string | null;
  reservedSeatIds?: number[] | null;
}

export interface RegisterWaitlistRequest {
  seatCount: number;
  requireAdjacent: boolean;
}
