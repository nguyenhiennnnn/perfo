import type { BookingStatus } from "@/types/enum";
export interface BookingMovieBriefDto {
  movieId: number;
  title: string;
  posterUrl?: string | null;
}

export interface BookingShowtimeBriefDto {
  showtimeId: number;
  roomName: string;
  startTime: string;
}

export interface BookingSeatDto {
  seatRow: string;
  seatNumber: number;
  price: number;
}

export interface BookingComboItemDto {
  name: string;
  quantity: number;
  unitPrice: number;
}

export interface BookingTicketDto {
  ticketId: number;
  seatRow: string;
  seatNumber: number;
  qrCodeImage: string;
  isCheckedIn: boolean;
  checkedInAt?: string | null;
}

export interface BookingPaymentDto {
  paymentId: number;
  paymentMethod: string;
  status: string;
  amount: number;
  transactionId?: string | null;
  paidAt?: string | null;
  createdAt: string;
  refundedAmount?: number | null;
  refundedAt?: string | null;
}

export interface AdminBookingListParams {
  status?: string;
  channel?: string;
  fromDate?: string;
  toDate?: string;
  search?: string;
  page?: number;
  pageSize?: number;
}

export interface AdminBookingListItemDto {
  bookingId: number;
  bookingCode: string;
  customerName: string;
  guestPhone?: string | null;
  finalAmount: number;
  status: BookingStatus;
  channel: string;
  createdAt: string;
}

export interface BookingEmployeeBriefDto {
  employeeId: number;
  fullName: string;
}

export interface AdminBookingDetailDto {
  bookingId: number;
  bookingCode: string;
  status: BookingStatus;
  movie: BookingMovieBriefDto;
  showtime: BookingShowtimeBriefDto;
  seats: BookingSeatDto[];
  combos: BookingComboItemDto[];
  totalAmount: number;
  discountAmount: number;
  finalAmount: number;
  createdAt: string;
  channel: string;
  createdByEmployee?: BookingEmployeeBriefDto | null;
  tickets?: BookingTicketDto[] | null;
  payments: BookingPaymentDto[];
}

export interface CounterHoldRequest {
  showtimeId: number;
  seatIds: number[];
  combos?: { comboId: number; quantity: number }[] | null;
  userId?: number | null;
  customerName: string;
  customerPhone?: string | null;
}

export interface CounterHoldResponse {
  bookingId: number;
  bookingCode: string;
  status: string;
  totalAmount: number;
  discountAmount: number;
  finalAmount: number;
  expiresAt: string;
  channel: string;
  createdByEmployeeId?: number | null;
}

export interface CounterCheckoutRequest {
  amountReceived: number;
}

export interface CounterCheckoutResponse {
  bookingId: number;
  status: string;
  change: number;
}

export interface CheckinRequest {
  qrCode: string;
}

export interface CheckinResponse {
  ticketId: number;
  bookingId: number;
  seatRow: string;
  seatNumber: number;
  movieTitle: string;
  roomName: string;
  startTime: string;
  checkedInAt: string;
}
