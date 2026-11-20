import type { Page } from "@playwright/test";
import { expect, test } from "./fixtures";
import { getAccessToken } from "./helpers/auth";
import {
  getSeatStatus,
  getShowtimeSeats,
  interceptVnPayRedirect,
  pickAdjacentSeats,
  SEAT_STATUS,
  seatCode,
  setHoldDurationMinutes,
  type ShowtimeSeat,
} from "./helpers/booking-flow";
import { sendIpn, signIpn } from "./helpers/ipn";
import {
  cancelShowtime,
  createShowtimeIn,
  SHOWTIME_OFFSETS_MINUTES,
  type CreatedShowtime,
} from "./helpers/showtimes";

const HOLD_BUTTON_NAME = "Giữ ghế & tiếp tục thanh toán";
const PAY_BUTTON_NAME = /^Thanh toán /;
const HOLD_EXPIRED_TOAST = "Thời gian giữ ghế đã hết hạn, vui lòng chọn lại.";
const HOLD_EXPIRED_TIMER_TEXT = "Đã hết thời gian giữ ghế.";
const IPN_SUCCESS_CODE = "00";

const SETUP_TIMEOUT_MS = 120_000;
const FULL_BOOKING_TEST_TIMEOUT_MS = 90_000;
const SHORT_HOLD_MINUTES = 1;
const SEATS_FOR_FULL_BOOKING = 2;
const SEATS_FOR_EXPIRY = 1;

/** Hạn giữ ghế 1 phút + job ReleaseExpiredSeats chạy mỗi phút + chu kỳ poll 10 giây của frontend. */
const EXPIRY_TEST_TIMEOUT_MS = 240_000;
const TIMER_EXPIRED_WAIT_MS = 90_000;
const SERVER_EXPIRY_WAIT_MS = 150_000;
const SEAT_RELEASE_WAIT_MS = 30_000;
const PAGE_SETTLE_WAIT_MS = 15_000;

interface PreparedShowtime {
  showtime: CreatedShowtime;
  seats: ShowtimeSeat[];
}

const seatButton = (page: Page, seat: ShowtimeSeat, state: string) =>
  page.getByRole("button", { name: `Ghế ${seatCode(seat)}, ${state}`, exact: true });

test.describe("Đặt vé trọn vẹn (C1)", () => {
  let prepared: PreparedShowtime;
  let adminToken: string;

  test.beforeAll(async ({ playwright }) => {
    test.setTimeout(SETUP_TIMEOUT_MS);
    const api = await playwright.request.newContext();
    try {
      adminToken = await getAccessToken(api, "admin");
      const showtime = await createShowtimeIn(api, adminToken, SHOWTIME_OFFSETS_MINUTES.in3h);
      const allSeats = await getShowtimeSeats(api, showtime.showtimeId);
      prepared = { showtime, seats: pickAdjacentSeats(allSeats, SEATS_FOR_FULL_BOOKING) };
    } finally {
      await api.dispose();
    }
  });

  test.afterAll(async ({ playwright }) => {
    if (!prepared) return;
    const api = await playwright.request.newContext();
    try {
      await cancelShowtime(api, adminToken, prepared.showtime.showtimeId);
    } catch {
      // Suất đã có đơn xác nhận nên backend có thể từ chối hủy; DB E2E riêng nên bỏ qua.
    } finally {
      await api.dispose();
    }
  });

  test("khách hàng chọn ghế, giữ ghế, thanh toán VNPay (IPN giả lập) và nhận vé điện tử", async ({
    customerPage: page,
    request,
  }) => {
    test.setTimeout(FULL_BOOKING_TEST_TIMEOUT_MS);
    const { showtime, seats } = prepared;
    const codes = seats.map(seatCode);

    await test.step("Mở trang đặt vé của suất chiếu", async () => {
      await page.goto(`/booking/${showtime.showtimeId}`);
      await expect(seatButton(page, seats[0], "trống")).toBeVisible({ timeout: PAGE_SETTLE_WAIT_MS });
      await expect(page.getByRole("button", { name: HOLD_BUTTON_NAME, exact: true })).toBeDisabled();
    });

    await test.step("Chọn ghế", async () => {
      for (const seat of seats) {
        await seatButton(page, seat, "trống").click();
        await expect(seatButton(page, seat, "đang chọn")).toHaveAttribute("aria-pressed", "true");
      }
      await expect(page.getByRole("button", { name: HOLD_BUTTON_NAME, exact: true })).toBeEnabled();
    });

    await test.step("Giữ ghế và sang bước thanh toán", async () => {
      await page.getByRole("button", { name: HOLD_BUTTON_NAME, exact: true }).click();
      await expect(page.getByRole("timer")).toBeVisible();
      await expect(page.getByRole("button", { name: PAY_BUTTON_NAME })).toBeEnabled();
    });

    const redirectPromise = await interceptVnPayRedirect(page);
    await test.step("Thanh toán và bị chuyển sang cổng VNPay", async () => {
      await page.getByRole("button", { name: PAY_BUTTON_NAME }).click();
    });
    const redirect = await redirectPromise;
    expect(redirect.txnRef).not.toBe("");
    expect(redirect.amount).toBeGreaterThan(0);
    expect(redirect.bookingId).not.toBe("");

    await test.step("VNPay gọi IPN thành công về backend", async () => {
      const response = await sendIpn(request, signIpn({ txnRef: redirect.txnRef, amount: redirect.amount }));
      expect(response.status).toBe(200);
      expect(response.rspCode).toBe(IPN_SUCCESS_CODE);
    });

    await test.step("Quay về trang kết quả: thanh toán thành công và có vé điện tử", async () => {
      await page.goto(redirect.returnPath);
      await expect(page.getByRole("heading", { name: "Thanh toán thành công", level: 1 })).toBeVisible({
        timeout: PAGE_SETTLE_WAIT_MS,
      });
      await expect(page.getByText(/^Mã đặt vé: /)).toBeVisible();
      for (const code of codes) {
        await expect(page.getByRole("group", { name: new RegExp(`ghế ${code}$`) })).toBeVisible();
      }
    });

    await test.step("Đơn xuất hiện trong Vé của tôi", async () => {
      const bookingCode = (await page.getByText(/^Mã đặt vé: /).innerText()).replace(/^Mã đặt vé:\s*/, "");
      await page.getByRole("link", { name: "Xem vé của tôi" }).click();
      await expect(page).toHaveURL(/\/account\/tickets$/);
      await expect(page.getByText(bookingCode, { exact: true }).first()).toBeVisible({
        timeout: PAGE_SETTLE_WAIT_MS,
      });
    });

    await test.step("Ghế đã chuyển sang trạng thái đã bán", async () => {
      for (const seat of seats) {
        await expect
          .poll(() => getSeatStatus(request, showtime.showtimeId, seat.showtimeSeatId), {
            message: `ghế ${seatCode(seat)} phải ở trạng thái Booked`,
            timeout: SEAT_RELEASE_WAIT_MS,
          })
          .toBe(SEAT_STATUS.booked);
      }
    });
  });
});

test.describe("Hết hạn giữ ghế (C5)", () => {
  let prepared: PreparedShowtime;
  let adminToken: string;
  let originalHoldMinutes: number | undefined;

  test.beforeAll(async ({ playwright }) => {
    test.setTimeout(SETUP_TIMEOUT_MS);
    const api = await playwright.request.newContext();
    try {
      adminToken = await getAccessToken(api, "admin");
      originalHoldMinutes = await setHoldDurationMinutes(api, adminToken, SHORT_HOLD_MINUTES);
      const showtime = await createShowtimeIn(api, adminToken, SHOWTIME_OFFSETS_MINUTES.in3h);
      const allSeats = await getShowtimeSeats(api, showtime.showtimeId);
      prepared = { showtime, seats: pickAdjacentSeats(allSeats, SEATS_FOR_EXPIRY) };
    } finally {
      await api.dispose();
    }
  });

  test.afterAll(async ({ playwright }) => {
    const api = await playwright.request.newContext();
    try {
      if (originalHoldMinutes !== undefined) {
        await setHoldDurationMinutes(api, adminToken, originalHoldMinutes);
      }
      if (prepared) await cancelShowtime(api, adminToken, prepared.showtime.showtimeId);
    } catch {
      // Dọn dẹp best-effort: cài đặt đã khôi phục ở bước đầu; suất chiếu E2E để lại cũng không ảnh hưởng.
    } finally {
      await api.dispose();
    }
  });

  test("giữ ghế quá hạn thì ghế được nhả và khách phải chọn lại", async ({
    customerPage: page,
    request,
  }) => {
    test.setTimeout(EXPIRY_TEST_TIMEOUT_MS);
    const { showtime, seats } = prepared;
    const [seat] = seats;

    await test.step("Giữ ghế và sang bước thanh toán", async () => {
      await page.goto(`/booking/${showtime.showtimeId}`);
      await seatButton(page, seat, "trống").click();
      await page.getByRole("button", { name: HOLD_BUTTON_NAME, exact: true }).click();
      await expect(page.getByRole("timer")).toBeVisible({ timeout: PAGE_SETTLE_WAIT_MS });
      await expect
        .poll(() => getSeatStatus(request, showtime.showtimeId, seat.showtimeSeatId), {
          message: "ghế vừa giữ phải ở trạng thái Held",
        })
        .toBe(SEAT_STATUS.held);
    });

    await test.step("Đồng hồ giữ ghế chạy về 0", async () => {
      await expect(page.getByRole("timer")).toContainText(HOLD_EXPIRED_TIMER_TEXT, {
        timeout: TIMER_EXPIRED_WAIT_MS,
      });
    });

    await test.step("Backend hết hạn đơn: có thông báo và quay lại bước chọn ghế", async () => {
      await expect(page.getByText(HOLD_EXPIRED_TOAST).first()).toBeVisible({
        timeout: SERVER_EXPIRY_WAIT_MS,
      });
      await expect(page.getByRole("button", { name: HOLD_BUTTON_NAME, exact: true })).toBeVisible({
        timeout: PAGE_SETTLE_WAIT_MS,
      });
      await expect(page.getByRole("timer")).toHaveCount(0);
      await expect(seatButton(page, seat, "trống")).toBeVisible({ timeout: PAGE_SETTLE_WAIT_MS });
    });

    await test.step("Ghế được nhả trên backend", async () => {
      await expect
        .poll(() => getSeatStatus(request, showtime.showtimeId, seat.showtimeSeatId), {
          message: "ghế hết hạn giữ phải trở về Available",
          timeout: SEAT_RELEASE_WAIT_MS,
        })
        .toBe(SEAT_STATUS.available);
    });
  });
});
