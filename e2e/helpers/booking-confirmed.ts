import type { APIRequestContext } from "@playwright/test";
import { API_BASE_URL } from "./auth";
import {
  BOOKING_STATUS,
  checkoutViaApi,
  getBookingDetail,
  holdSeatsViaApi,
} from "./booking-api";
import { pickAdjacentSeats, seatCode, type ShowtimeSeat } from "./booking-flow";
import { sendIpn, signIpn } from "./ipn";

/**
 * Hỗ trợ cho các test hủy vé/hoàn tiền: tạo đơn đã thanh toán (Confirmed) bằng API,
 * soát vé tại quầy, đọc chính sách hủy và gọi hủy đơn để xem phản hồi lỗi.
 */

const IPN_CONFIRMED_CODE = "00";

interface ApiEnvelope<T> {
  success: boolean;
  data: T;
}

export interface CancelPolicy {
  /** % hoàn khi hủy hợp lệ (CancelRefundPercent). */
  cancelRefundPercent: number;
  /** Phải hủy trước giờ chiếu ít nhất chừng này giờ (CancelDeadlineHours). */
  cancelDeadlineHours: number;
}

export interface ConfirmedBooking {
  bookingId: number;
  bookingCode: string;
  /** vnp_TxnRef = Payment.TransactionId, dùng để đối chiếu yêu cầu hoàn tiền ở mock VNPay. */
  txnRef: string;
  /** Số tiền khách đã thanh toán (VND). */
  amount: number;
  seat: ShowtimeSeat;
}

export interface CancelAttempt {
  status: number;
  /** Nguyên body phản hồi (chuỗi) để kiểm tra thông điệp lỗi của backend. */
  text: string;
}

/** Chọn mỗi ghế ở một hàng khác nhau để các đơn trong cùng suất không dính luật "ghế lẻ" của nhau. */
export function pickSeatsInDistinctRows(seats: ShowtimeSeat[], count: number): ShowtimeSeat[] {
  const picked: ShowtimeSeat[] = [];
  for (let i = 0; i < count; i++) {
    const usedRows = new Set(picked.map((seat) => seat.seatRow));
    const [seat] = pickAdjacentSeats(
      seats.filter((candidate) => !usedRows.has(candidate.seatRow)),
      1,
    );
    picked.push(seat);
  }
  return picked;
}

/** Chính sách hủy/hoàn tiền hiện hành (GET /settings, công khai). */
export async function getCancelPolicy(request: APIRequestContext): Promise<CancelPolicy> {
  const response = await request.get(`${API_BASE_URL}/settings`, { failOnStatusCode: false });
  if (!response.ok()) {
    throw new Error(`Đọc cài đặt công khai thất bại: HTTP ${response.status()} ${await response.text()}`);
  }
  const data = ((await response.json()) as ApiEnvelope<Record<string, unknown>>).data;
  return {
    cancelRefundPercent: Number(data.cancelRefundPercent),
    cancelDeadlineHours: Number(data.cancelDeadlineHours),
  };
}

/** Giữ ghế, tạo giao dịch VNPay rồi gọi IPN thành công: đơn chuyển sang Confirmed và có vé. */
export async function createConfirmedBooking(
  request: APIRequestContext,
  customerToken: string,
  showtimeId: number,
  seat: ShowtimeSeat,
): Promise<ConfirmedBooking> {
  const hold = await holdSeatsViaApi(request, customerToken, {
    showtimeId,
    seatIds: [seat.showtimeSeatId],
  });
  const payment = await checkoutViaApi(request, customerToken, hold.bookingId);

  const ipn = await sendIpn(request, signIpn({ txnRef: payment.txnRef, amount: payment.amount }));
  if (ipn.rspCode !== IPN_CONFIRMED_CODE) {
    throw new Error(`IPN thanh toán đơn ${hold.bookingId} không được xác nhận: ${ipn.rspCode} ${ipn.message}`);
  }

  const detail = await getBookingDetail(request, customerToken, hold.bookingId);
  if (detail.status !== BOOKING_STATUS.confirmed) {
    throw new Error(`Đơn ${hold.bookingId} sau IPN có trạng thái ${detail.status}, kỳ vọng Confirmed.`);
  }

  return {
    bookingId: hold.bookingId,
    bookingCode: hold.bookingCode,
    txnRef: payment.txnRef,
    amount: payment.amount,
    seat,
  };
}

/** Nội dung mã QR của vé = "{mã đơn}-{hàng}{số ghế}" (CodeGenerator.GenerateTicketQrPayload). */
export function ticketQrPayload(booking: Pick<ConfirmedBooking, "bookingCode" | "seat">): string {
  return `${booking.bookingCode}-${seatCode(booking.seat)}`;
}

/** Soát vé tại quầy (POST /admin/bookings/checkin; nhân viên hoặc admin đều được). */
export async function checkinTicketViaApi(
  request: APIRequestContext,
  staffToken: string,
  qrCode: string,
): Promise<void> {
  const response = await request.post(`${API_BASE_URL}/admin/bookings/checkin`, {
    headers: { Authorization: `Bearer ${staffToken}` },
    data: { qrCode },
    failOnStatusCode: false,
  });
  if (!response.ok()) {
    throw new Error(`Soát vé ${qrCode} thất bại: HTTP ${response.status()} ${await response.text()}`);
  }
}

/** Gọi hủy đơn của khách và trả về cả status lẫn body, không ném lỗi theo HTTP status. */
export async function attemptCancelBooking(
  request: APIRequestContext,
  customerToken: string,
  bookingId: number,
): Promise<CancelAttempt> {
  const response = await request.post(`${API_BASE_URL}/bookings/${bookingId}/cancel`, {
    headers: { Authorization: `Bearer ${customerToken}` },
    failOnStatusCode: false,
  });
  // ASP.NET Core mặc định escape ký tự non-ASCII (\uXXXX): parse rồi stringify lại để so khớp tiếng Việt.
  const raw = await response.text();
  let text = raw;
  try {
    text = JSON.stringify(JSON.parse(raw));
  } catch {
    // Body không phải JSON: giữ nguyên chuỗi gốc.
  }
  return { status: response.status(), text };
}
