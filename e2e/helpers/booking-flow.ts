import type { APIRequestContext, Page } from "@playwright/test";
import { API_BASE_URL } from "./auth";

/**
 * Hỗ trợ cho các test luồng đặt vé (đặt vé trọn vẹn, hết hạn giữ ghế):
 *  - đọc sơ đồ ghế công khai và chọn ghế không vi phạm quy tắc "ghế lẻ";
 *  - đọc/đổi thời gian giữ ghế (Cài đặt) qua API admin;
 *  - chặn lần chuyển hướng sang cổng VNPay và lấy tham số thanh toán.
 */

export const SEAT_STATUS = {
  available: "Available",
  held: "Held",
  booked: "Booked",
} as const;

export interface ShowtimeSeat {
  showtimeSeatId: number;
  seatRow: string;
  seatNumber: number;
  seatType?: string | null;
  isCouple?: boolean;
  status: string;
  price: number;
}

interface ApiEnvelope<T> {
  success: boolean;
  data: T;
}

interface ShowtimeSeatsData {
  showtimeId: number;
  seats: ShowtimeSeat[];
}

/** Các khóa chỉ có ở phản hồi GET /admin/settings, không thuộc UpdateSettingsRequest. */
const SETTINGS_RESPONSE_ONLY_KEYS = [
  "timeZoneId",
  "locale",
  "currencyCode",
  "currencySymbol",
  "defaultEmployeePassword",
] as const;

/** Số ghế liền nhau còn trống cần có thêm ở mỗi bên cụm chọn để không tạo ghế lẻ (chế độ Strict). */
const GAP_MARGIN_SEATS = 2;

function authHeaders(accessToken: string) {
  return { Authorization: `Bearer ${accessToken}` };
}

/** Mã hiển thị của ghế, khớp aria-label "Ghế A5, trống" trên sơ đồ ghế. */
export function seatCode(seat: Pick<ShowtimeSeat, "seatRow" | "seatNumber">): string {
  return `${seat.seatRow}${seat.seatNumber}`;
}

/** Đọc sơ đồ ghế của suất chiếu (GET /showtimes/{id}/seats, endpoint công khai). */
export async function getShowtimeSeats(
  request: APIRequestContext,
  showtimeId: number,
): Promise<ShowtimeSeat[]> {
  const response = await request.get(`${API_BASE_URL}/showtimes/${showtimeId}/seats`, {
    failOnStatusCode: false,
  });
  if (!response.ok()) {
    throw new Error(`Đọc sơ đồ ghế suất ${showtimeId} thất bại: HTTP ${response.status()} ${await response.text()}`);
  }
  return ((await response.json()) as ApiEnvelope<ShowtimeSeatsData>).data.seats;
}

/** Trạng thái hiện tại ("Available" | "Held" | "Booked") của một ghế trong suất chiếu. */
export async function getSeatStatus(
  request: APIRequestContext,
  showtimeId: number,
  showtimeSeatId: number,
): Promise<string | undefined> {
  const seats = await getShowtimeSeats(request, showtimeId);
  return seats.find((seat) => seat.showtimeSeatId === showtimeSeatId)?.status;
}

/**
 * Chọn `count` ghế thường liền nhau ở giữa một hàng còn trống hoàn toàn, chừa mỗi bên
 * ít nhất 2 ghế trống liền kề để không dính luật "không để trống ghế lẻ" (SeatGapRuleMode.Strict).
 */
export function pickAdjacentSeats(seats: ShowtimeSeat[], count: number): ShowtimeSeat[] {
  const windowSize = count + GAP_MARGIN_SEATS * 2;
  const rows = new Map<string, ShowtimeSeat[]>();
  for (const seat of seats) {
    if (seat.isCouple || seat.status !== SEAT_STATUS.available) continue;
    rows.set(seat.seatRow, [...(rows.get(seat.seatRow) ?? []), seat]);
  }

  for (const rowSeats of rows.values()) {
    const sorted = [...rowSeats].sort((a, b) => a.seatNumber - b.seatNumber);
    for (let start = 0; start + windowSize <= sorted.length; start++) {
      const windowSeats = sorted.slice(start, start + windowSize);
      const isContiguous = windowSeats.every(
        (seat, i) => i === 0 || seat.seatNumber === windowSeats[i - 1].seatNumber + 1,
      );
      if (isContiguous) return windowSeats.slice(GAP_MARGIN_SEATS, GAP_MARGIN_SEATS + count);
    }
  }

  throw new Error(
    `Không tìm được ${count} ghế thường liền nhau (kèm ${GAP_MARGIN_SEATS} ghế trống mỗi bên) trong suất chiếu E2E.`,
  );
}

/**
 * Đặt thời gian giữ ghế (phút, 1-120) bằng PUT /admin/settings và trả về giá trị cũ để khôi phục.
 * PUT cần đủ trường nên đọc GET /admin/settings rồi gửi lại nguyên vẹn, chỉ đổi holdDurationMinutes.
 * Đây là cài đặt dùng chung của rạp: chỉ chạy trên DB E2E riêng và luôn khôi phục sau test.
 */
export async function setHoldDurationMinutes(
  request: APIRequestContext,
  adminAccessToken: string,
  minutes: number,
): Promise<number> {
  const getResponse = await request.get(`${API_BASE_URL}/admin/settings`, {
    headers: authHeaders(adminAccessToken),
    failOnStatusCode: false,
  });
  if (!getResponse.ok()) {
    throw new Error(`Đọc cài đặt rạp thất bại: HTTP ${getResponse.status()} ${await getResponse.text()}`);
  }
  const current = ((await getResponse.json()) as ApiEnvelope<Record<string, unknown>>).data;
  const previous = Number(current.holdDurationMinutes);

  const body: Record<string, unknown> = { ...current, holdDurationMinutes: minutes };
  for (const key of SETTINGS_RESPONSE_ONLY_KEYS) delete body[key];

  const putResponse = await request.put(`${API_BASE_URL}/admin/settings`, {
    headers: authHeaders(adminAccessToken),
    data: body,
    failOnStatusCode: false,
  });
  if (!putResponse.ok()) {
    throw new Error(
      `Đặt thời gian giữ ghế ${minutes} phút thất bại: HTTP ${putResponse.status()} ${await putResponse.text()}`,
    );
  }
  return previous;
}

export interface VnPayRedirect {
  /** Số tiền VND (đã chia 100 từ vnp_Amount), dùng cho signIpn. */
  amount: number;
  /** vnp_TxnRef = Payment.TransactionId, dùng cho signIpn. */
  txnRef: string;
  /** Đường dẫn + query của vnp_ReturnUrl (ví dụ /booking/result?bookingId=12), mở lại bằng baseURL. */
  returnPath: string;
  bookingId: string;
}

const VNPAY_AMOUNT_SCALE = 100;

/**
 * Chặn lần điều hướng sang cổng VNPay (URL luôn có vnp_SecureHash) để test không chạm sandbox thật,
 * trả về Promise sẽ resolve khi frontend chuyển hướng. Gọi TRƯỚC khi bấm nút "Thanh toán".
 */
export async function interceptVnPayRedirect(page: Page): Promise<Promise<VnPayRedirect>> {
  let resolveRedirect!: (redirect: VnPayRedirect) => void;
  const redirect = new Promise<VnPayRedirect>((resolve) => {
    resolveRedirect = resolve;
  });

  await page.route(
    (url) => url.searchParams.has("vnp_SecureHash"),
    async (route) => {
      const url = new URL(route.request().url());
      const returnUrl = new URL(url.searchParams.get("vnp_ReturnUrl") ?? "");
      resolveRedirect({
        amount: Number(url.searchParams.get("vnp_Amount")) / VNPAY_AMOUNT_SCALE,
        txnRef: url.searchParams.get("vnp_TxnRef") ?? "",
        returnPath: `${returnUrl.pathname}${returnUrl.search}`,
        bookingId: returnUrl.searchParams.get("bookingId") ?? "",
      });
      await route.fulfill({
        status: 200,
        contentType: "text/html; charset=utf-8",
        body: "<!doctype html><title>VNPay giả lập</title><h1>VNPay giả lập</h1>",
      });
    },
  );

  return redirect;
}
