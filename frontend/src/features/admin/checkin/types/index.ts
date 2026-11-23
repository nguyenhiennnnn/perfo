export interface CheckinRequest {
  qrCode: string;
}

export interface CheckinResponse {
  orderLineId: number;
  orderId: number;
  orderCode: string;
  seatRow: string;
  seatNumber: number;
  movieTitle: string;
  roomName: string;
  startTime: string;
  checkedInAt: string;
  ticketTypeId?: number | null;
  ticketTypeName?: string | null;
  requiresCardPresentation: boolean;
  cardCheckStatus: CardCheckStatus;
}

export const CardCheckStatus = {
  NotRequired: "NotRequired",
  Pending: "Pending",
  NotPresented: "NotPresented",
  Supplemented: "Supplemented",
} as const;
export type CardCheckStatus = (typeof CardCheckStatus)[keyof typeof CardCheckStatus];

export interface RecordCardNotPresentedResponse {
  orderLineId: number;
  orderId: number;
  cardCheckStatus: CardCheckStatus;
  cardCheckRecordedAt: string;
}

export interface CounterSupplementOrderRequest {
  orderLineId: number;
  replacementTicketTypeId?: number | null;
}

export type CheckinErrorKind = "duplicate" | "notFound" | "invalid" | "unknown";

export interface CheckinFailure {
  kind: CheckinErrorKind;
  message: string;
  previous?: CheckinResponse | null;
}
