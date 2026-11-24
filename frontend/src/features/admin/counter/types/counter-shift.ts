export const COUNTER_SHIFT_MAX_CASH = 1_000_000_000;
export const COUNTER_SHIFT_NOTE_MAX_LENGTH = 500;

export interface CounterShift {
  counterShiftId: number;
  employeeId: number;
  employeeName: string;
  openedAt: string;
  openingCash: number;
  closedAt: string | null;
  countedCash: number | null;
  expectedCash: number | null;
  variance: number | null;
  note: string | null;
  status: string;
}

export interface OpenCounterShiftRequest {
  openingCash: number;
}

export interface CloseCounterShiftRequest {
  countedCash: number;
  note?: string | null;
}
