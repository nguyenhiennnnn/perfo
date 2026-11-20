import type { APIRequestContext } from "@playwright/test";
import { API_BASE_URL } from "./auth";

/**
 * Gọi thẳng API đặt vé của khách (không qua giao diện) cho các test thanh toán/IPN:
 *  - giữ ghế (POST /bookings/hold) và tạo giao dịch VNPay (POST /bookings/{id}/checkout);
 *  - đọc chi tiết đơn (GET /bookings/{id}) để đối chiếu trạng thái đơn, thanh toán và vé;
 *  - hủy đơn đang chờ (POST /bookings/{id}/cancel) để dọn sau test.
 * Chữ ký IPN và việc gửi IPN nằm ở ./ipn; sơ đồ ghế ở ./booking-flow.
 */

/** Trạng thái đơn (BookingStatus.ToDtoStatus). */
export const BOOKING_STATUS = {
  pending: "Pending",
  confirmed: "Confirmed",
  cancelled: "Cancelled",
  expired: "Expired",
} as const;

/** Trạng thái thanh toán (PaymentStatus.ToDtoStatus). */
export const PAYMENT_STATUS = {
  pending: "Pending",
  success: "Success",
  failed: "Failed",
  refunded: "Refunded",
} as const;

/** Tên phương thức thanh toán backend nhận (PaymentMethodNames.VnPay). */
export const VNPAY_METHOD = "VNPay";

const DEFAULT_CUSTOMER_NAME = "Khách E2E";
const VNPAY_AMOUNT_SCALE = 100;
const HTTP_CREATED = 201;

interface ApiEnvelope<T> {
  success: boolean;
  data: T;
}

export interface HoldResult {
  bookingId: number;
  bookingCode: string;
  status: string;
  finalAmount: number;
  /** ISO 8601 UTC. */
  expiresAt: string;
}

export interface CheckoutResult {
  paymentId: number;
  paymentUrl: string;
  /** vnp_TxnRef trong paymentUrl = Payment.TransactionId, dùng cho signIpn. */
  txnRef: string;
  /** Số tiền VND (đã chia 100 từ vnp_Amount), dùng cho signIpn. */
  amount: number;
}

export interface BookingPayment {
  paymentId: number;
  paymentMethod: string;
  status: string;
  amount: number;
  transactionId?: string | null;
  paidAt?: string | null;
  refundedAmount?: number | null;
  refundedAt?: string | null;
}

export interface BookingTicket {
  ticketId: number;
  seatRow: string;
  seatNumber: number;
  isCheckedIn: boolean;
}

export interface BookingDetail {
  bookingId: number;
  bookingCode: string;
  status: string;
  finalAmount: number;
  seats: { seatRow: string; seatNumber: number; price: number }[];
  tickets: BookingTicket[];
  /** Mới nhất trước (backend sắp xếp theo CreatedAt giảm dần). */
  payments: BookingPayment[];
  /** Chỉ có khi đơn còn Pending. */
  holdExpiresAt?: string | null;
}

export interface HoldSeatsInput {
  showtimeId: number;
  /** Các showtimeSeatId (không phải mã ghế A5). */
  seatIds: number[];
  customerName?: string;
}

function authHeaders(accessToken: string) {
  return { Authorization: `Bearer ${accessToken}` };
}

/** Giữ ghế bằng API. Ném lỗi nếu backend không trả 201 (ví dụ ghế đã có người giữ -> 409). */
export async function holdSeatsViaApi(
  request: APIRequestContext,
  accessToken: string,
  input: HoldSeatsInput,
): Promise<HoldResult> {
  const response = await request.post(`${API_BASE_URL}/bookings/hold`, {
    headers: authHeaders(accessToken),
    data: {
      showtimeId: input.showtimeId,
      customerName: input.customerName ?? DEFAULT_CUSTOMER_NAME,
      seatIds: input.seatIds,
      combos: [],
    },
    failOnStatusCode: false,
  });
  if (response.status() !== HTTP_CREATED) {
    throw new Error(
      `Giữ ghế ${input.seatIds.join(",")} (suất ${input.showtimeId}) thất bại: HTTP ${response.status()} ${await response.text()}`,
    );
  }
  return ((await response.json()) as ApiEnvelope<HoldResult>).data;
}

/**
 * Tạo giao dịch VNPay cho đơn đang giữ ghế và tách vnp_TxnRef, vnp_Amount từ paymentUrl
 * (đúng thứ trình duyệt thấy khi bị chuyển sang cổng VNPay). Gọi lại lần nữa là thanh toán lại:
 * backend đổi các giao dịch Pending cũ sang Failed và tạo giao dịch mới.
 */
export async function checkoutViaApi(
  request: APIRequestContext,
  accessToken: string,
  bookingId: number,
): Promise<CheckoutResult> {
  const response = await request.post(`${API_BASE_URL}/bookings/${bookingId}/checkout`, {
    headers: authHeaders(accessToken),
    data: { paymentMethod: VNPAY_METHOD },
    failOnStatusCode: false,
  });
  if (!response.ok()) {
    throw new Error(
      `Thanh toán đơn ${bookingId} thất bại: HTTP ${response.status()} ${await response.text()}`,
    );
  }

  const { paymentUrl, paymentId } = ((await response.json()) as ApiEnvelope<{
    paymentUrl: string;
    paymentId: number;
  }>).data;
  const params = new URL(paymentUrl).searchParams;
  const txnRef = params.get("vnp_TxnRef") ?? "";
  if (!txnRef) throw new Error(`paymentUrl của đơn ${bookingId} không có vnp_TxnRef.`);

  return {
    paymentId,
    paymentUrl,
    txnRef,
    amount: Number(params.get("vnp_Amount")) / VNPAY_AMOUNT_SCALE,
  };
}

/** Đọc chi tiết đơn của chính khách (GET /bookings/{id}). */
export async function getBookingDetail(
  request: APIRequestContext,
  accessToken: string,
  bookingId: number,
): Promise<BookingDetail> {
  const response = await request.get(`${API_BASE_URL}/bookings/${bookingId}`, {
    headers: authHeaders(accessToken),
    failOnStatusCode: false,
  });
  if (!response.ok()) {
    throw new Error(
      `Đọc đơn ${bookingId} thất bại: HTTP ${response.status()} ${await response.text()}`,
    );
  }
  return ((await response.json()) as ApiEnvelope<BookingDetail>).data;
}

/** Hủy đơn (Pending thì nhả ghế). Dùng để dọn; không ném lỗi theo HTTP status, trả về status. */
export async function cancelBookingViaApi(
  request: APIRequestContext,
  accessToken: string,
  bookingId: number,
): Promise<number> {
  const response = await request.post(`${API_BASE_URL}/bookings/${bookingId}/cancel`, {
    headers: authHeaders(accessToken),
    failOnStatusCode: false,
  });
  return response.status();
}
