import type { Page } from "@playwright/test";
import { expect, test } from "./fixtures";
import { getAccessToken } from "./helpers/auth";
import { BOOKING_STATUS } from "./helpers/booking-api";
import { ticketQrPayload } from "./helpers/booking-confirmed";
import { getShowtimeSeats, pickAdjacentSeats, seatCode, type ShowtimeSeat } from "./helpers/booking-flow";
import { createCounterTicket, getAdminBookingDetail, getCheckInOpenMinutes } from "./helpers/counter";
import {
  cancelShowtime,
  createRelativeShowtimes,
  type CreatedShowtime,
  type RelativeSlot,
} from "./helpers/showtimes";

const SETUP_TIMEOUT_MS = 240_000;
const UI_TEST_TIMEOUT_MS = 90_000;
const PAGE_SETTLE_WAIT_MS = 15_000;

const CHECKIN_PATH = "/admin/checkin";
const SCAN_BUTTON_NAME = "Soát vé ngay";
const SUCCESS_TITLE = "Check-in thành công";
const FAILURE_TITLE = "Soát vé thất bại";
const ALREADY_CHECKED_IN_MESSAGE = "Vé đã check-in trước đó.";
const NOT_OPEN_YET_MESSAGE = (minutes: number) => `Chưa tới giờ check-in (mở trước giờ chiếu ${minutes} phút).`;
const NO_LONGER_VALID_MESSAGE = "Vé không còn hiệu lực (booking đã hủy hoặc chưa thanh toán).";
const INVALID_QR_MESSAGE = "QR không hợp lệ.";

const MIN_OPEN_WINDOW_MINUTES = 20;
const MAX_OPEN_WINDOW_MINUTES = 45;
const SEATS_PER_SLOT = 2;

let adminToken: string;
let employeeToken: string;
let checkInOpenMinutes: number;
let showtimes: Record<RelativeSlot, CreatedShowtime>;
let seatsBySlot: Record<RelativeSlot, ShowtimeSeat[]>;

const successCard = (page: Page) => page.getByRole("status").filter({ hasText: SUCCESS_TITLE });
const failureCard = (page: Page) => page.getByRole("alert").filter({ hasText: FAILURE_TITLE });

async function scanTicket(page: Page, code: string) {
  await page.getByTestId("checkin-code-input").fill(code);
  await page.getByRole("button", { name: SCAN_BUTTON_NAME, exact: true }).click();
}

function pickSeats(allSeats: ShowtimeSeat[], count: number): ShowtimeSeat[] {
  const picked: ShowtimeSeat[] = [];
  for (let i = 0; i < count; i++) {
    const usedRows = new Set(picked.map((seat) => seat.seatRow));
    picked.push(pickAdjacentSeats(allSeats.filter((seat) => !usedRows.has(seat.seatRow)), 1)[0]);
  }
  return picked;
}

test.describe("Soát vé và check-in tại quầy (F7-F9)", () => {
  test.beforeAll(async ({ playwright }) => {
    test.setTimeout(SETUP_TIMEOUT_MS);
    const api = await playwright.request.newContext();
    try {
      adminToken = await getAccessToken(api, "admin");
      employeeToken = await getAccessToken(api, "employee");

      checkInOpenMinutes = await getCheckInOpenMinutes(api, adminToken);
      if (checkInOpenMinutes < MIN_OPEN_WINDOW_MINUTES || checkInOpenMinutes > MAX_OPEN_WINDOW_MINUTES) {
        throw new Error(
          `Bộ test check-in giả định cửa sổ check-in từ ${MIN_OPEN_WINDOW_MINUTES} đến ${MAX_OPEN_WINDOW_MINUTES} phút ` +
            `(suất còn 20 phút phải mở, suất còn 1 giờ phải chưa mở), hiện là ${checkInOpenMinutes}. ` +
            "Chỉnh lại Cài đặt rạp của DB E2E.",
        );
      }

      showtimes = await createRelativeShowtimes(api, adminToken);
      seatsBySlot = {} as Record<RelativeSlot, ShowtimeSeat[]>;
      for (const slot of Object.keys(showtimes) as RelativeSlot[]) {
        const allSeats = await getShowtimeSeats(api, showtimes[slot].showtimeId);
        seatsBySlot[slot] = pickSeats(allSeats, SEATS_PER_SLOT);
      }
    } finally {
      await api.dispose();
    }
  });

  test.afterAll(async ({ playwright }) => {
    if (!showtimes) return;
    const api = await playwright.request.newContext();
    try {
      for (const slot of Object.keys(showtimes) as RelativeSlot[]) {
        try {
          await cancelShowtime(api, adminToken, showtimes[slot].showtimeId);
        } catch {
          continue;
        }
      }
    } finally {
      await api.dispose();
    }
  });

  test("quét vé hợp lệ khi cửa sổ check-in đang mở thì check-in thành công và hiện đúng ghế, phòng, phim", async ({
    employeePage: page,
    request,
  }) => {
    test.setTimeout(UI_TEST_TIMEOUT_MS);
    const showtime = showtimes.in20m;
    const seat = seatsBySlot.in20m[0];
    const ticket = await createCounterTicket(request, employeeToken, showtime.showtimeId, seat);

    await test.step("Mở trang soát vé và quét mã vé", async () => {
      await page.goto(CHECKIN_PATH);
      await scanTicket(page, ticketQrPayload(ticket));
    });

    await test.step("Thẻ kết quả báo check-in thành công kèm ghế, phòng, phim", async () => {
      const card = successCard(page);
      await expect(card).toBeVisible({ timeout: PAGE_SETTLE_WAIT_MS });
      await expect(card).toContainText(seatCode(seat));
      await expect(card).toContainText(showtime.roomName);
      await expect(card).toContainText(showtime.movieTitle);
      await expect(failureCard(page)).toHaveCount(0);
    });

    await test.step("Vé được đánh dấu đã check-in và có thời điểm check-in", async () => {
      const detail = await getAdminBookingDetail(request, employeeToken, ticket.bookingId);
      expect(detail.status).toBe(BOOKING_STATUS.confirmed);
      expect(detail.tickets).toHaveLength(1);
      expect(detail.tickets[0].isCheckedIn).toBe(true);
      expect(detail.tickets[0].checkedInAt).toBeTruthy();
    });
  });

  test("quét lại vé đã check-in thì bị từ chối vì đã check-in trước đó và không đổi thời điểm check-in", async ({
    employeePage: page,
    request,
  }) => {
    test.setTimeout(UI_TEST_TIMEOUT_MS);
    const showtime = showtimes.in20m;
    const ticket = await createCounterTicket(request, employeeToken, showtime.showtimeId, seatsBySlot.in20m[1]);
    const code = ticketQrPayload(ticket);
    let firstCheckedInAt: string | null | undefined;

    await test.step("Quét lần 1: check-in thành công", async () => {
      await page.goto(CHECKIN_PATH);
      await scanTicket(page, code);
      await expect(successCard(page)).toBeVisible({ timeout: PAGE_SETTLE_WAIT_MS });

      const detail = await getAdminBookingDetail(request, employeeToken, ticket.bookingId);
      expect(detail.tickets[0].isCheckedIn).toBe(true);
      firstCheckedInAt = detail.tickets[0].checkedInAt;
      expect(firstCheckedInAt).toBeTruthy();
    });

    await test.step("Quét lần 2: báo thất bại, vé đã check-in trước đó", async () => {
      await scanTicket(page, code);
      const card = failureCard(page);
      await expect(card).toBeVisible({ timeout: PAGE_SETTLE_WAIT_MS });
      await expect(card).toContainText(ALREADY_CHECKED_IN_MESSAGE);
      await expect(successCard(page)).toHaveCount(0);
    });

    await test.step("Thời điểm check-in của vé không bị ghi đè", async () => {
      const detail = await getAdminBookingDetail(request, employeeToken, ticket.bookingId);
      expect(detail.tickets[0].isCheckedIn).toBe(true);
      expect(detail.tickets[0].checkedInAt).toBe(firstCheckedInAt);
    });
  });

  test("quét vé quá sớm (suất còn 1 giờ, chưa mở check-in) thì bị từ chối và vé chưa được check-in", async ({
    employeePage: page,
    request,
  }) => {
    test.setTimeout(UI_TEST_TIMEOUT_MS);
    const showtime = showtimes.in1h;
    const ticket = await createCounterTicket(request, employeeToken, showtime.showtimeId, seatsBySlot.in1h[0]);

    await test.step("Mở trang soát vé và quét mã vé", async () => {
      await page.goto(CHECKIN_PATH);
      await scanTicket(page, ticketQrPayload(ticket));
    });

    await test.step("Báo thất bại: chưa tới giờ check-in, kèm số phút mở trước giờ chiếu", async () => {
      const card = failureCard(page);
      await expect(card).toBeVisible({ timeout: PAGE_SETTLE_WAIT_MS });
      await expect(card).toContainText(NOT_OPEN_YET_MESSAGE(checkInOpenMinutes));
      await expect(successCard(page)).toHaveCount(0);
    });

    await test.step("Đơn vẫn Confirmed và vé chưa check-in", async () => {
      const detail = await getAdminBookingDetail(request, employeeToken, ticket.bookingId);
      expect(detail.status).toBe(BOOKING_STATUS.confirmed);
      expect(detail.tickets[0].isCheckedIn).toBe(false);
    });
  });

  test("quét vé của suất đã bị hủy thì bị từ chối vì vé không còn hiệu lực và vé chưa được check-in", async ({
    employeePage: page,
    request,
  }) => {
    test.setTimeout(UI_TEST_TIMEOUT_MS);
    const showtime = showtimes.in3h;
    const ticket = await createCounterTicket(request, employeeToken, showtime.showtimeId, seatsBySlot.in3h[0]);

    await test.step("Admin hủy suất chiếu: đơn của vé chuyển sang Cancelled", async () => {
      await cancelShowtime(request, adminToken, showtime.showtimeId);
      const detail = await getAdminBookingDetail(request, employeeToken, ticket.bookingId);
      expect(detail.status).toBe(BOOKING_STATUS.cancelled);
    });

    await test.step("Mở trang soát vé và quét mã vé", async () => {
      await page.goto(CHECKIN_PATH);
      await scanTicket(page, ticketQrPayload(ticket));
    });

    await test.step("Báo thất bại: vé không còn hiệu lực", async () => {
      const card = failureCard(page);
      await expect(card).toBeVisible({ timeout: PAGE_SETTLE_WAIT_MS });
      await expect(card).toContainText(NO_LONGER_VALID_MESSAGE);
      await expect(successCard(page)).toHaveCount(0);
    });

    await test.step("Vé không được đánh dấu đã check-in", async () => {
      const detail = await getAdminBookingDetail(request, employeeToken, ticket.bookingId);
      expect(detail.tickets[0].isCheckedIn).toBe(false);
    });
  });

  test("quét mã sai (không tồn tại) thì báo QR không hợp lệ", async ({ employeePage: page }) => {
    test.setTimeout(UI_TEST_TIMEOUT_MS);
    const wrongCode = `E2E-MA-SAI-${Date.now()}`;

    await test.step("Mở trang soát vé và quét mã không tồn tại", async () => {
      await page.goto(CHECKIN_PATH);
      await scanTicket(page, wrongCode);
    });

    await test.step("Báo thất bại: QR không hợp lệ", async () => {
      const card = failureCard(page);
      await expect(card).toBeVisible({ timeout: PAGE_SETTLE_WAIT_MS });
      await expect(card).toContainText(INVALID_QR_MESSAGE);
      await expect(successCard(page)).toHaveCount(0);
    });
  });
});
