export interface AdminShowtimeListParams {
  roomId?: number;
  movieId?: number;
  date?: string;
  status?: string;
  page?: number;
  pageSize?: number;
}

export interface AdminShowtimeDto {
  showtimeId: number;
  movieId: number;
  movieTitle: string;
  roomId: number;
  roomName: string;
  startTime: string;
  endTime: string;
  basePrice: number;
  status: string;
}

export interface UpsertShowtimeRequest {
  movieId: number;
  roomId: number;
  startTime: string;
  basePrice: number;
  allowAfterHours?: boolean;
}

export interface CreateShowtimeResponse {
  showtimeId: number;
  movieId: number;
  movieTitle: string;
  roomId: number;
  roomName: string;
  startTime: string;
  endTime: string;
  basePrice: number;
  status: string;
  seatsGenerated: number;
}

export interface DeleteShowtimeResponse {
  showtimeId: number;
  status: string;
  manualRefundRequiredCount: number;
  autoRefundQueuedCount?: number;
}

export interface BulkDateRange {
  from: string;
  to: string;
}

export interface BulkCreateShowtimesRequest {
  movieId: number;
  roomId: number;
  dateRange: BulkDateRange;
  daysOfWeek: string[];
  times: string[];
  basePrice: number;
  allowAfterHours?: boolean;
}

export interface BulkCreatedShowtimeItem {
  showtimeId: number;
  startTime: string;
  seatsGenerated: number;
}

export interface BulkSkippedShowtimeItem {
  date: string;
  time: string;
  reason: string;
}

export interface BulkCreateShowtimesResponse {
  created: BulkCreatedShowtimeItem[];
  skipped: BulkSkippedShowtimeItem[];
  totalCreated: number;
  totalSkipped: number;
}

export interface BulkDeleteShowtimesRequest {
  showtimeIds: number[];
}

export interface BulkDeleteShowtimesResultItem {
  showtimeId: number;
  success: boolean;
  reason?: string | null;
}

export interface BulkDeleteShowtimesResponse {
  results: BulkDeleteShowtimesResultItem[];
  totalSuccess: number;
  totalFailed: number;
}

export interface AdminShowtimeCalendarParams {
  from: string;
  to: string;
  roomId?: number;
  movieId?: number;
  status?: string;
}

export interface ShowtimeCalendarItemDto {
  showtimeId: number;
  movieId: number;
  movieTitle: string;
  moviePosterUrl?: string | null;
  roomId: number;
  roomName: string;
  screenType?: string | null;
  startTime: string;
  endTime: string;
  basePrice: number;
  availableSeats: number;
  totalSeats: number;
  status: string;
}

export interface ShowtimeCalendarDayDto {
  date: string;
  showtimes: ShowtimeCalendarItemDto[];
}

export interface ShowtimeCalendarClosureDto {
  roomClosureId: number;
  roomId: number;
  roomName: string;
  startTime: string;
  endTime: string;
  reason: string;
}

export interface ShowtimeCalendarResponseDto {
  from: string;
  to: string;
  days: ShowtimeCalendarDayDto[];
  closures?: ShowtimeCalendarClosureDto[];
}

export interface AutoScheduleMovieRequest {
  movieId: number;
  shows: number;
  basePrice?: number;
}

export interface AutoScheduleRequest {
  date: string;
  movies: AutoScheduleMovieRequest[];
  basePrice: number;
  roomIds?: number[];
  allowAfterHours?: boolean;
  toDate?: string;
}

export interface AutoScheduleItemDto {
  movieId: number;
  movieTitle: string;
  roomId: number;
  roomName: string;
  startTime: string;
  endTime: string;
  basePrice: number;
}

export interface AutoScheduleUnplacedDto {
  movieId: number;
  movieTitle: string;
  requested: number;
  placed: number;
}

export interface AutoScheduleResponseDto {
  date: string;
  toDate?: string | null;
  items: AutoScheduleItemDto[];
  unplaced: AutoScheduleUnplacedDto[];
}

export interface ApplyAutoScheduleRequest {
  items: UpsertShowtimeRequest[];
}

export interface CloneShowtimesRequest {
  sourceFrom: string;
  sourceTo: string;
  shiftDays: number;
  roomIds?: number[];
  allowAfterHours?: boolean;
}

export type ShowtimeCancelReason = "ShowtimeCancelled" | "RoomClosed";

export type RoomChangeMatchKind = "Exact" | "Nearest" | "Manual" | "Unmapped";

export interface RoomChangeSeatOverride {
  showtimeSeatId: number;
  targetSeatId: number;
}

export interface ShowtimeRoomChangeRequest {
  targetRoomId: number;
  overrides?: RoomChangeSeatOverride[];
  note?: string | null;
}

export interface RoomChangeSeatRef {
  row: string;
  number: number;
  seatTypeId?: number | null;
  seatType?: string | null;
  isCouple: boolean;
}

export interface RoomChangeTargetSeat extends RoomChangeSeatRef {
  seatId: number;
  coupleId?: string | null;
}

export interface RoomChangeSeatMapping {
  showtimeSeatId: number;
  status: string;
  hasOrders: boolean;
  source: RoomChangeSeatRef;
  targetSeatId?: number | null;
  target?: RoomChangeSeatRef | null;
  matchKind: RoomChangeMatchKind;
  needsReview: boolean;
  refundDue: number;
}

export interface RoomChangePreviewResponse {
  showtimeId: number;
  sourceRoomId: number;
  sourceRoomName: string;
  targetRoomId: number;
  targetRoomName: string;
  canApply: boolean;
  issues: string[];
  occupiedSeatCount: number;
  targetSeatCount: number;
  needsReviewCount: number;
  refundDueCount: number;
  refundDueTotal: number;
  mappings: RoomChangeSeatMapping[];
  targetSeats: RoomChangeTargetSeat[];
}

export interface ShowtimeRoomChangeResponse {
  showtimeRoomChangeId: number;
  showtimeId: number;
  fromRoomId: number;
  fromRoomName: string;
  toRoomId: number;
  toRoomName: string;
  seatsMoved: number;
  orderCount: number;
  refundDueCount: number;
  refundDueTotal: number;
}


export const SeatBlockType = {
  Maintenance: "Maintenance",
  Broken: "Broken",
  RoomRental: "RoomRental",
  WaitlistReserve: "WaitlistReserve",
} as const;
export type SeatBlockType = (typeof SeatBlockType)[keyof typeof SeatBlockType];

export interface SeatBlockDto {
  seatBlockId: number;
  showtimeId: number;
  showtimeSeatId: number;
  seatRow: string;
  seatNumber: number;
  type: string;
  reason: string;
  expiresAt: string | null;
  createdByUserId: number | null;
  createdAt: string;
}

export interface BlockSeatsRequest {
  showtimeSeatIds: number[];
  type: string;
  reason: string;
  expiresAt?: string | null;
}

export interface UnblockSeatsRequest {
  showtimeSeatIds: number[];
}

export interface UnblockSeatsResponse {
  showtimeId: number;
  showtimeSeatIds: number[];
}


export type ShowtimeEventAdjustmentMode = "Multiplier" | "Amount";

export interface ShowtimeEventDto {
  showtimeId: number;
  adjustmentMode: ShowtimeEventAdjustmentMode;
  adjustmentValue: number;
  allowMembershipFreeTickets: boolean;
}

export interface UpsertShowtimeEventRequest {
  adjustmentMode: ShowtimeEventAdjustmentMode;
  adjustmentValue: number;
  allowMembershipFreeTickets: boolean;
}

export interface ShowtimeEventTicketQuotaRequest {
  ticketTypeId: number;
  maxTickets: number;
}

export interface UpsertShowtimeEventLimitsRequest {
  maxTicketsPerCustomer: number | null;
  ticketQuotas: ShowtimeEventTicketQuotaRequest[];
}

export interface ShowtimeEventTicketQuotaDto {
  ticketTypeId: number;
  ticketTypeName: string;
  maxTickets: number;
  usedTickets: number;
}

export interface ShowtimeEventLimitsDto {
  showtimeId: number;
  maxTicketsPerCustomer: number | null;
  ticketQuotas: ShowtimeEventTicketQuotaDto[];
}

export interface ShowtimeEventGiftRequest {
  inventoryItemId: number;
  quantity: number;
  quantityPerTicket: number;
}

export interface UpsertShowtimeEventGiftsRequest {
  gifts: ShowtimeEventGiftRequest[];
}

export interface ShowtimeEventGiftDto {
  inventoryItemId: number;
  code: string;
  name: string;
  unit: string;
  quantity: number;
  quantityPerTicket: number;
  held: number;
  given: number;
}

export interface ShowtimeEventGiftsDto {
  showtimeId: number;
  gifts: ShowtimeEventGiftDto[];
}

export interface UpsertShowtimeEarlyAccessRequest {
  generalSaleStartTime: string;
  membershipTierIds: number[];
  paidMembers: boolean;
  movieInterest: boolean;
}

export interface ShowtimeEarlyAccessSettingDto {
  showtimeId: number;
  generalSaleStartTime: string | null;
  membershipTierIds: number[];
  paidMembers: boolean;
  movieInterest: boolean;
}
