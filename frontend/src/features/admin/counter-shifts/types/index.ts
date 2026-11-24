import type { CounterShift } from "@/features/admin/counter/types/counter-shift";

export type { CounterShift };

export interface CounterShiftListParams {
  page?: number;
  pageSize?: number;
  employeeId?: string;
  status?: string;
  fromDate?: string;
  toDate?: string;
}

export interface OpenCounterShiftItem {
  shift: CounterShift;
  isOverdue: boolean;
  alertedAt: string | null;
}

export interface AdminCloseCounterShiftRequest {
  countedCash: number;
  note: string;
}

export const COUNTER_SHIFT_STATUS_OPEN = "Open";
export const COUNTER_SHIFT_STATUS_CLOSED = "Closed";
