import type { APIRequestContext, PlaywrightWorkerArgs } from "@playwright/test";
import { expect, test } from "./fixtures";
import { ACCOUNTS, getAccessToken } from "./helpers/auth";
import {
  BOOKING_STATUS,
  getBookingDetail,
  PAYMENT_STATUS,
} from "./helpers/booking-api";
import {
  attemptCancelBooking,
  checkinTicketViaApi,
  createConfirmedBooking,
  getCancelPolicy,
  pickSeatsInDistinctRows,
  ticketQrPayload,
  type CancelPolicy,
} from "./helpers/booking-confirmed";
import { getSeatStatus, getShowtimeSeats, SEAT_STATUS, type ShowtimeSeat } from "./helpers/booking-flow";
import { updateCinemaSettings } from "./helpers/cinema-settings";
import { startMockGateway, type MockGateway } from "./helpers/mock-gateway";
import {
  cancelShowtime,
  createShowtimeIn,
  SHOWTIME_OFFSETS_MINUTES,
  type CreatedShowtime,
} from "./helpers/showtimes";

const SETUP_TIMEOUT_MS = 180_000;
const CLEANUP_TIMEOUT_MS = 90_000;
const UI_TEST_TIMEOUT_MS = 150_000;
/** Hoàn tiền + gửi mail chạy qua Hangfire nên cần chờ, không kiểm tra ngay. */
const ASYNC_TEST_TIMEOUT_MS = 240_000;
const MOCK_CALL_WAIT_MS = 15_000;
const EMAIL_WAIT_MS = 90_000;
const REFUND_WAIT_MS = 90_000;
const CLEANUP_REFUND_WAIT_MS = 45_000;
const POLL_INTERVALS_MS = [1_000, 2_000, 3_000];

/** CancelDeadlineHours mà bộ test này giả định (mặc định của rạp): +3 giờ hủy được, +1 giờ thì quá hạn. */
const EXPECTED_DEADLINE_HOURS = 2;
/** Cửa sổ check-in tối đa của rạp (phút, 0-240) để soát vé được ngay cả với suất còn 3 giờ. */
const MAX_CHECKIN_OPEN_MINUTES = 240;
const HTTP_UNPROCESSABLE = 422;

const CANCEL_BUTTON_NAME = "Hủy vé & Hoàn tiền";
const CANCEL_CONFIRM_NAME = "Hủy vé";
const CANCEL_SHOWTIME_NAME = "Hủy suất chiếu";
const BOOKING_CONFIRMED_LABEL = "Đã xác nhận";
const BOOKING_CANCELLED_LABEL = "Đã hủy";
const CHECKED_IN_TICKET_LABEL = "Đã soát vé";
const MOCK_HINT =
  "Backend phải chạy với VnPay__ApiUrl và Email__ResendApiKey/Email__ResendApiUrl trỏ tới mock gateway (xem .env.e2e.example).";

type Playwright = PlaywrightWorkerArgs["playwright"];

interface PreparedShowtime {
  showtime: CreatedShowtime;
  seats: ShowtimeSeat[];
}

interface Tokens {
  admin: string;
  customer: string;
}

async function prepareShowtime(
  playwright: Playwright,
  adminToken: string,
  offsetMinutes: number,
  seatCount: number,
  excludeRoomIds: number[],
): Promise<PreparedShowtime> {
  const api = await playwright.request.newContext();
  try {
    const showtime = await createShowtimeIn(api, adminToken, offsetMinutes, { excludeRoomIds });
    const seats = pickSeatsInDistinctRows(await getShowtimeSeats(api, showtime.showtimeId), seatCount);
    return { showtime, seats };
  } finally {
    await api.dispose();
  }
}

let mock: MockGateway;
let tokens: Tokens;
let policy: CancelPolicy;
/** Suất +3 giờ cho E1 và E3 (mỗi test một ghế ở hàng riêng). */
let refundable: PreparedShowtime;
/** Suất +1 giờ cho E2 (quá hạn hoàn 2 giờ). */
let pastDeadline: PreparedShowtime;
/** Suất +3 giờ riêng cho G4 vì test này hủy cả suất. */
let adminCancelled: PreparedShowtime;
/** Giao dịch của các đơn còn Confirmed sau test, để chờ job hoàn tiền khi dọn suất chiếu. */
const txnRefsToRefundOnCleanup: string[] = [];

test.beforeAll(async ({ playwright }) => {
  test.setTimeout(SETUP_TIMEOUT_MS);
  mock = await startMockGateway();

  const api = await playwright.request.newContext();
  try {
    // Đăng nhập một lần cho cả file: endpoint auth bị rate limit 5 lần / 60 giây theo IP.
    tokens = {
      admin: await getAccessToken(api, "admin"),
      customer: await getAccessToken(api, "customer"),
    };
    policy = await getCancelPolicy(api);
  } finally {
    await api.dispose();
  }

  if (policy.cancelRefundPercent <= 0 || policy.cancelDeadlineHours !== EXPECTED_DEADLINE_HOURS) {
    throw new Error(
      `Bộ test hủy vé giả định hoàn tiền > 0% và hạn hủy ${EXPECTED_DEADLINE_HOURS} giờ, hiện là ` +
        `${policy.cancelRefundPercent}% / ${policy.cancelDeadlineHours} giờ. Chỉnh lại Cài đặt rạp của DB E2E.`,
    );
  }

  refundable = await prepareShowtime(playwright, tokens.admin, SHOWTIME_OFFSETS_MINUTES.in3h, 2, []);
  pastDeadline = await prepareShowtime(playwright, tokens.admin, SHOWTIME_OFFSETS_MINUTES.in1h, 1, [
    refundable.showtime.roomId,
  ]);
  adminCancelled = await prepareShowtime(playwright, tokens.admin, SHOWTIME_OFFSETS_MINUTES.in3h, 1, [
    refundable.showtime.roomId,
    pastDeadline.showtime.roomId,
  ]);
});

test.afterAll(async ({ playwright }) => {
  test.setTimeout(CLEANUP_TIMEOUT_MS);
  if (!mock) return;

  try {
    if (tokens) {
      const api = await playwright.request.newContext();
      try {
        for (const prepared of [refundable, pastDeadline, adminCancelled]) {
          if (!prepared) continue;
          try {
            await cancelShowtime(api, tokens.admin, prepared.showtime.showtimeId);
          } catch {
            // Suất đã bị hủy (G4) hoặc backend từ chối; DB E2E riêng nên bỏ qua.
          }
        }
      } finally {
        await api.dispose();
      }

      // Hủy suất sẽ xếp job hoàn tiền cho các đơn còn Confirmed: chờ job chạy xong rồi mới tắt mock.
      const deadline = Date.now() + CLEANUP_REFUND_WAIT_MS;
      while (
        txnRefsToRefundOnCleanup.some((txnRef) => !mock.findRefund(txnRef)) &&
        Date.now() < deadline
      ) {
        await new Promise((resolve) => setTimeout(resolve, 2_000));
      }
    }
  } finally {
    await mock.close();
  }
});

const customerEmail = ACCOUNTS.customer.email;

async function expectSeatStatus(
  request: APIRequestContext,
  prepared: PreparedShowtime,
  seat: ShowtimeSeat,
  expected: string,
) {
  expect(await getSeatStatus(request, prepared.showtime.showtimeId, seat.showtimeSeatId)).toBe(expected);
}

test.describe("Hủy vé và hoàn tiền (E1, E2, E3)", () => {
  test("E1: khách hủy vé khi còn hơn 2 giờ trước giờ chiếu thì đơn bị hủy, hoàn tiền qua VNPay, nhả ghế và nhận email xác nhận", async ({
    customerPage: page,
    request,
  }) => {
    test.setTimeout(ASYNC_TEST_TIMEOUT_MS);
    const seat = refundable.seats[0];
    const booking = await createConfirmedBooking(
      request,
      tokens.customer,
      refundable.showtime.showtimeId,
      seat,
    );
    const expectedRefund = Math.round((booking.amount * policy.cancelRefundPercent) / 100);

    await test.step("Mở chi tiết đơn: đơn Đã xác nhận và có nút hủy vé", async () => {
      await page.goto(`/account/tickets/${booking.bookingId}`);
      await expect(page.getByTestId("ticket-detail-status")).toHaveText(BOOKING_CONFIRMED_LABEL);
      await expect(page.getByRole("button", { name: CANCEL_BUTTON_NAME })).toBeVisible();
    });

    await test.step("Hủy vé và xác nhận trong hộp thoại", async () => {
      await page.getByRole("button", { name: CANCEL_BUTTON_NAME }).click();
      const dialog = page.getByRole("alertdialog");
      await expect(dialog).toBeVisible();
      await dialog.getByRole("button", { name: CANCEL_CONFIRM_NAME, exact: true }).click();

      await expect(page.getByTestId("ticket-detail-status")).toHaveText(BOOKING_CANCELLED_LABEL);
      await expect(page.getByRole("button", { name: CANCEL_BUTTON_NAME })).toHaveCount(0);
    });

    await test.step("Backend: đơn Cancelled, giao dịch Refunded đúng số tiền hoàn theo chính sách", async () => {
      const detail = await getBookingDetail(request, tokens.customer, booking.bookingId);
      expect(detail.status).toBe(BOOKING_STATUS.cancelled);
      expect(detail.payments[0].status).toBe(PAYMENT_STATUS.refunded);
      expect(detail.payments[0].refundedAmount).toBe(expectedRefund);
      expect(detail.payments[0].refundedAt).toBeTruthy();
    });

    await test.step("Ghế được nhả về Available", async () => {
      await expectSeatStatus(request, refundable, seat, SEAT_STATUS.available);
    });

    await test.step("VNPay (mock) nhận đúng một yêu cầu hoàn tiền cho giao dịch gốc", async () => {
      await expect
        .poll(() => mock.findRefund(booking.txnRef)?.amount, {
          message: `Mock VNPay không nhận được yêu cầu hoàn tiền. ${MOCK_HINT}`,
          timeout: MOCK_CALL_WAIT_MS,
        })
        .toBe(expectedRefund);
      expect(mock.refunds.filter((refund) => refund.txnRef === booking.txnRef)).toHaveLength(1);
    });

    await test.step("Outbox (mock Resend) có email xác nhận hủy vé & hoàn tiền gửi cho khách", async () => {
      await expect
        .poll(
          () =>
            mock.findEmail({
              to: customerEmail,
              subject: /^Xác nhận hủy vé & hoàn tiền/,
              bookingCode: booking.bookingCode,
            }) !== undefined,
          {
            message: `Không thấy email xác nhận hủy vé trong outbox. ${MOCK_HINT}`,
            timeout: EMAIL_WAIT_MS,
            intervals: POLL_INTERVALS_MS,
          },
        )
        .toBe(true);
    });
  });

  test("E2: hủy vé khi còn dưới 2 giờ (quá hạn hoàn) bị từ chối, không hoàn tiền và vé vẫn hợp lệ", async ({
    customerPage: page,
    request,
  }) => {
    test.setTimeout(UI_TEST_TIMEOUT_MS);
    const seat = pastDeadline.seats[0];
    const booking = await createConfirmedBooking(
      request,
      tokens.customer,
      pastDeadline.showtime.showtimeId,
      seat,
    );
    txnRefsToRefundOnCleanup.push(booking.txnRef);

    await test.step("Giao diện: không có nút hủy vé, hiện thông báo quá hạn", async () => {
      await page.goto(`/account/tickets/${booking.bookingId}`);
      await expect(page.getByTestId("ticket-detail-status")).toHaveText(BOOKING_CONFIRMED_LABEL);
      await expect(page.getByTestId("ticket-cancel-notice")).toContainText("Đã quá hạn hủy vé");
      await expect(page.getByRole("button", { name: CANCEL_BUTTON_NAME })).toHaveCount(0);
    });

    await test.step("API: gọi hủy trực tiếp vẫn bị backend từ chối vì quá hạn hoàn tiền", async () => {
      const attempt = await attemptCancelBooking(request, tokens.customer, booking.bookingId);
      expect(attempt.status).toBe(HTTP_UNPROCESSABLE);
      expect(attempt.text).toContain("quá hạn hoàn tiền");
    });

    await test.step("Đơn vẫn Confirmed, giao dịch vẫn Success, ghế vẫn Booked, VNPay không bị gọi hoàn tiền", async () => {
      const detail = await getBookingDetail(request, tokens.customer, booking.bookingId);
      expect(detail.status).toBe(BOOKING_STATUS.confirmed);
      expect(detail.payments[0].status).toBe(PAYMENT_STATUS.success);
      expect(detail.tickets).toHaveLength(1);
      await expectSeatStatus(request, pastDeadline, seat, SEAT_STATUS.booked);
      expect(mock.findRefund(booking.txnRef)).toBeUndefined();
    });
  });

  test("E3: vé đã check-in thì không được hủy dù còn hơn 2 giờ, không hoàn tiền", async ({
    customerPage: page,
    request,
  }) => {
    test.setTimeout(UI_TEST_TIMEOUT_MS);
    const seat = refundable.seats[1];
    const booking = await createConfirmedBooking(
      request,
      tokens.customer,
      refundable.showtime.showtimeId,
      seat,
    );
    txnRefsToRefundOnCleanup.push(booking.txnRef);

    await test.step("Mở cửa sổ check-in tạm thời (suất còn 3 giờ) rồi soát vé tại quầy, sau đó khôi phục cài đặt", async () => {
      const previous = await updateCinemaSettings(request, tokens.admin, {
        checkInOpenMinutes: MAX_CHECKIN_OPEN_MINUTES,
      });
      try {
        await checkinTicketViaApi(request, tokens.admin, ticketQrPayload(booking));
      } finally {
        await updateCinemaSettings(request, tokens.admin, {
          checkInOpenMinutes: previous.checkInOpenMinutes,
        });
      }
    });

    await test.step("Giao diện: vé hiện Đã soát vé; bấm hủy thì backend từ chối và báo lỗi", async () => {
      await page.goto(`/account/tickets/${booking.bookingId}`);
      await expect(page.getByTestId("ticket-detail-status")).toHaveText(BOOKING_CONFIRMED_LABEL);
      await expect(page.getByText(CHECKED_IN_TICKET_LABEL)).toBeVisible();

      await page.getByRole("button", { name: CANCEL_BUTTON_NAME }).click();
      await page
        .getByRole("alertdialog")
        .getByRole("button", { name: CANCEL_CONFIRM_NAME, exact: true })
        .click();
      await expect(page.getByText(/đã có vé được check-in/).first()).toBeVisible();
      await expect(page.getByTestId("ticket-detail-status")).toHaveText(BOOKING_CONFIRMED_LABEL);
    });

    await test.step("API: hủy trực tiếp cũng bị từ chối với thông điệp check-in", async () => {
      const attempt = await attemptCancelBooking(request, tokens.customer, booking.bookingId);
      expect(attempt.status).toBe(HTTP_UNPROCESSABLE);
      expect(attempt.text).toContain("check-in");
    });

    await test.step("Đơn vẫn Confirmed, vé đã check-in, giao dịch Success, không có yêu cầu hoàn tiền", async () => {
      const detail = await getBookingDetail(request, tokens.customer, booking.bookingId);
      expect(detail.status).toBe(BOOKING_STATUS.confirmed);
      expect(detail.payments[0].status).toBe(PAYMENT_STATUS.success);
      expect(detail.tickets).toHaveLength(1);
      expect(detail.tickets[0].isCheckedIn).toBe(true);
      expect(mock.findRefund(booking.txnRef)).toBeUndefined();
    });
  });
});

test.describe("Admin hủy suất chiếu có đơn đã xác nhận (G4)", () => {
  test("G4: admin hủy suất thì đơn Confirmed bị hủy, hoàn tiền toàn bộ tự động và khách nhận email", async ({
    adminPage: page,
    request,
  }) => {
    test.setTimeout(ASYNC_TEST_TIMEOUT_MS);
    const { showtime, seats } = adminCancelled;
    const booking = await createConfirmedBooking(request, tokens.customer, showtime.showtimeId, seats[0]);

    await test.step("Admin mở danh sách suất chiếu của phòng và hủy suất bằng menu hành động", async () => {
      await page.goto(`/admin/showtimes?roomId=${showtime.roomId}&status=Scheduled&pageSize=100`);
      const row = page.getByRole("row", { name: new RegExp(`#${showtime.showtimeId}(?!\\d)`) });
      await expect(row).toBeVisible();

      await row.getByRole("button", { name: "Mở menu" }).click();
      await page.getByRole("menuitem", { name: CANCEL_SHOWTIME_NAME }).click();

      const dialog = page.getByRole("alertdialog");
      await expect(dialog).toContainText(showtime.movieTitle);
      await dialog.getByRole("button", { name: CANCEL_SHOWTIME_NAME, exact: true }).click();

      await expect(page.getByText("1 giao dịch online đang được hoàn tiền tự động.")).toBeVisible();
      await expect(row).toHaveCount(0);
    });

    await test.step("Đơn đặt vé chuyển sang Cancelled ngay", async () => {
      const detail = await getBookingDetail(request, tokens.customer, booking.bookingId);
      expect(detail.status).toBe(BOOKING_STATUS.cancelled);
    });

    await test.step("Job hoàn tiền chạy: giao dịch Refunded toàn bộ số tiền đã thanh toán", async () => {
      await expect
        .poll(
          async () => (await getBookingDetail(request, tokens.customer, booking.bookingId)).payments[0]?.status,
          {
            message: "Giao dịch phải chuyển sang Refunded sau khi job hoàn tiền chạy",
            timeout: REFUND_WAIT_MS,
            intervals: POLL_INTERVALS_MS,
          },
        )
        .toBe(PAYMENT_STATUS.refunded);

      const detail = await getBookingDetail(request, tokens.customer, booking.bookingId);
      expect(detail.payments[0].refundedAmount).toBe(booking.amount);
      expect(detail.payments[0].refundedAt).toBeTruthy();
    });

    await test.step("VNPay (mock) nhận yêu cầu hoàn đủ số tiền của giao dịch gốc", async () => {
      await expect
        .poll(() => mock.findRefund(booking.txnRef)?.amount, {
          message: `Mock VNPay không nhận được yêu cầu hoàn tiền. ${MOCK_HINT}`,
          timeout: MOCK_CALL_WAIT_MS,
        })
        .toBe(booking.amount);
    });

    await test.step("Outbox (mock Resend) có email suất chiếu bị hủy và đã hoàn tiền gửi cho khách", async () => {
      await expect
        .poll(
          () =>
            mock.findEmail({
              to: customerEmail,
              subject: /^Suất chiếu bị hủy — đã hoàn tiền/,
              bookingCode: booking.bookingCode,
            }) !== undefined,
          {
            message: `Không thấy email suất chiếu bị hủy trong outbox. ${MOCK_HINT}`,
            timeout: EMAIL_WAIT_MS,
            intervals: POLL_INTERVALS_MS,
          },
        )
        .toBe(true);
    });
  });
});
