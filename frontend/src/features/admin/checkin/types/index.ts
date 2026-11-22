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
  ticketTypeName?: string | null;
}

export type CheckinErrorKind = "duplicate" | "notFound" | "invalid" | "unknown";

export interface CheckinFailure {
  kind: CheckinErrorKind;
  message: string;
  previous?: CheckinResponse | null;
}
