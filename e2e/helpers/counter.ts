import type { APIRequestContext } from "@playwright/test";
import { API_BASE_URL } from "./auth";
import type { BookingTicket } from "./booking-api";
import type { ShowtimeSeat } from "./booking-flow";

const HTTP_CREATED = 201;
const COUNTER_CUSTOMER_NAME = "Khách quầy E2E";
const AMOUNT_SEPARATOR = "[.,\\s\\u00a0\\u202f]?";

interface ApiEnvelope<T> {
  success: boolean;
  data: T;
  message?: string;
}

export interface CounterHold {
  bookingId: number;
  bookingCode: string;
  status: string;
  finalAmount: number;
}

export interface CounterCheckout {
  bookingId: number;
  status: string;
  change: number;
}

export interface CounterTicket {
  bookingId: number;
  bookingCode: string;
  finalAmount: number;
  seat: ShowtimeSeat;
}

export interface AdminBookingPayment {
  paymentId: number;
  paymentMethod: string;
  status: string;
  amount: number;
}

export interface AdminBookingDetail {
  bookingId: number;
  bookingCode: string;
  status: string;
  channel: string;
  finalAmount: number;
  tickets: (BookingTicket & { checkedInAt?: string | null })[];
  payments: AdminBookingPayment[];
}

export interface CounterCheckoutAttempt {
  status: number;
  message: string;
}

function authHeaders(token: string) {
  return { Authorization: `Bearer ${token}` };
}

export function amountPattern(amount: number): RegExp {
  const digits = String(amount).split("");
  return new RegExp(`(?<!\\d)${digits.join(AMOUNT_SEPARATOR)}(?!\\d)`);
}

export async function getCheckInOpenMinutes(
  request: APIRequestContext,
  adminToken: string,
): Promise<number> {
  const response = await request.get(`${API_BASE_URL}/admin/settings`, {
    headers: authHeaders(adminToken),
    failOnStatusCode: false,
  });
  if (!response.ok()) {
    throw new Error(`Đọc cài đặt rạp thất bại: HTTP ${response.status()} ${await response.text()}`);
  }
  const data = ((await response.json()) as ApiEnvelope<Record<string, unknown>>).data;
  return Number(data.checkInOpenMinutes);
}

export async function counterHoldViaApi(
  request: APIRequestContext,
  employeeToken: string,
  showtimeId: number,
  seat: ShowtimeSeat,
): Promise<CounterHold> {
  const response = await request.post(`${API_BASE_URL}/admin/bookings/counter-hold`, {
    headers: authHeaders(employeeToken),
    data: {
      showtimeId,
      seatIds: [seat.showtimeSeatId],
      customerName: COUNTER_CUSTOMER_NAME,
    },
    failOnStatusCode: false,
  });
  if (response.status() !== HTTP_CREATED) {
    throw new Error(
      `Giữ ghế quầy ${seat.seatRow}${seat.seatNumber} (suất ${showtimeId}) thất bại: HTTP ${response.status()} ${await response.text()}`,
    );
  }
  return ((await response.json()) as ApiEnvelope<CounterHold>).data;
}

export async function attemptCounterCheckout(
  request: APIRequestContext,
  employeeToken: string,
  bookingId: number,
  amountReceived: number,
): Promise<CounterCheckoutAttempt> {
  const response = await request.post(`${API_BASE_URL}/admin/bookings/${bookingId}/counter-checkout`, {
    headers: authHeaders(employeeToken),
    data: { amountReceived },
    failOnStatusCode: false,
  });
  let message = "";
  try {
    message = ((await response.json()) as ApiEnvelope<unknown>).message ?? "";
  } catch {
    message = "";
  }
  return { status: response.status(), message };
}

export async function createCounterTicket(
  request: APIRequestContext,
  employeeToken: string,
  showtimeId: number,
  seat: ShowtimeSeat,
): Promise<CounterTicket> {
  const hold = await counterHoldViaApi(request, employeeToken, showtimeId, seat);
  const attempt = await attemptCounterCheckout(request, employeeToken, hold.bookingId, hold.finalAmount);
  if (attempt.status !== 200) {
    throw new Error(
      `Thanh toán quầy đơn ${hold.bookingId} thất bại: HTTP ${attempt.status} ${attempt.message}`,
    );
  }
  return { bookingId: hold.bookingId, bookingCode: hold.bookingCode, finalAmount: hold.finalAmount, seat };
}

export async function getAdminBookingDetail(
  request: APIRequestContext,
  staffToken: string,
  bookingId: number,
): Promise<AdminBookingDetail> {
  const response = await request.get(`${API_BASE_URL}/admin/bookings/${bookingId}`, {
    headers: authHeaders(staffToken),
    failOnStatusCode: false,
  });
  if (!response.ok()) {
    throw new Error(`Đọc đơn ${bookingId} (admin) thất bại: HTTP ${response.status()} ${await response.text()}`);
  }
  return ((await response.json()) as ApiEnvelope<AdminBookingDetail>).data;
}

export async function counterCancelViaApi(
  request: APIRequestContext,
  employeeToken: string,
  bookingId: number,
): Promise<number> {
  const response = await request.post(`${API_BASE_URL}/admin/bookings/${bookingId}/counter-cancel`, {
    headers: authHeaders(employeeToken),
    failOnStatusCode: false,
  });
  return response.status();
}
