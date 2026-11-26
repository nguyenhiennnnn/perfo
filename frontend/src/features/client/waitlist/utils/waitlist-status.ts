import { WaitlistStatus } from "@/types/enum";
import type { WaitlistEntryDto } from "../types";

const EMPTY_SEAT_IDS: number[] = [];

export const getReservedSeatIds = (entry: WaitlistEntryDto): number[] =>
  entry.reservedSeatIds ?? EMPTY_SEAT_IDS;

export const isWaitlistPolling = (status: WaitlistStatus | undefined): boolean =>
  status === WaitlistStatus.Waiting || status === WaitlistStatus.Notified;

export const isWaitlistActive = (entry: WaitlistEntryDto | undefined): entry is WaitlistEntryDto =>
  !!entry &&
  (entry.status === WaitlistStatus.Waiting ||
    entry.status === WaitlistStatus.Notified ||
    entry.status === WaitlistStatus.Held);
