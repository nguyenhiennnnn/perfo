import type { Page } from "@playwright/test";
import { expect, test } from "./fixtures";
import { getAccessToken } from "./helpers/auth";
import { BOOKING_STATUS, PAYMENT_STATUS } from "./helpers/booking-api";
import { getShowtimeSeats, pickAdjacentSeats, seatCode, type ShowtimeSeat } from "./helpers/booking-flow";
import {
  amountPattern,
  attemptCounterCheckout,
  counterCancelViaApi,
  getAdminBookingDetail,
  type CounterCheckout,
  type CounterHold,
} from "./helpers/counter";
import { cancelShowtime, createShowtimeIn, SHOWTIME_OFFSETS_MINUTES, type CreatedShowtime } from "./helpers/showtimes";

const SETUP_TIMEOUT_MS = 180_000;
const UI_TEST_TIMEOUT_MS = 90_000;
const PAGE_SETTLE_WAIT_MS = 15_000;

const COUNTER_PATH = "/admin/bookings/counter";
const CUSTOMER_NAME = "Khách quầy E2E";
const HOLD_BUTTON_NAME = "Giữ ghế & tiếp tục";
const CONFIRM_BUTTON_NAME = "Xác nhận thanh toán";
const AMOUNT_RECEIVED_LABEL = "Số tiền khách đưa (tiền mặt / chuyển khoản)";
const CUSTOMER_NAME_LABEL = "Tên khách hàng";
const INSUFFICIENT_UI_MESSAGE = "Số tiền khách đưa chưa đủ so với số tiền cần thanh toán.";
const INSUFFICIENT_API_MESSAGE = "Số tiền nhận không đủ.";
const SUCCESS_HEADING = "Thanh toán thành công";
const CHANGE_RESULT_PATTERN = /^Tiền thừa trả khách:/;
const CASH_METHOD = "Cash";

const SHORT_BY_AMOUNT = 1_000;
const CHANGE_AMOUNT = 15_000;
const HTTP_UNPROCESSABLE = 422;

interface ApiEnvelope<T> {
  data: T;
}

let adminToken: string;
let employeeToken: string;
let showtime: CreatedShowtime;
let seats: ShowtimeSeat[];

const seatButton = (page: Page, seat: ShowtimeSeat) =>
  page.getByRole("button", { name: `Ghế ${seatCode(seat)}, trống`, exact: true });

async function holdSeatAtCounter(page: Page, seat: ShowtimeSeat): Promise<CounterHold> {
  await page.goto(COUNTER_PATH);
  await page.getByTestId(`counter-showtime-${showtime.showtimeId}`).click({ timeout: PAGE_SETTLE_WAIT_MS });
  await seatButton(page, seat).click({ timeout: PAGE_SETTLE_WAIT_MS });
  await page.getByLabel(CUSTOMER_NAME_LABEL, { exact: true }).fill(CUSTOMER_NAME);

  const holdButton = page.getByRole("button", { name: HOLD_BUTTON_NAME, exact: true });
  await expect(holdButton).toBeEnabled();

  const holdResponse = page.waitForResponse(
    (response) => response.url().includes("/admin/bookings/counter-hold") && response.request().method() === "POST",
  );
  await holdButton.click();
  const hold = ((await (await holdResponse).json()) as ApiEnvelope<CounterHold>).data;

  await expect(page.getByLabel(AMOUNT_RECEIVED_LABEL, { exact: true })).toBeVisible({ timeout: PAGE_SETTLE_WAIT_MS });
  return hold;
}

test.describe("Bán vé tại quầy bằng tiền mặt (F1-F2)", () => {
  test.beforeAll(async ({ playwright }) => {
    test.setTimeout(SETUP_TIMEOUT_MS);
    const api = await playwright.request.newContext();
    try {
      adminToken = await getAccessToken(api, "admin");
      employeeToken = await getAccessToken(api, "employee");
      showtime = await createShowtimeIn(api, adminToken, SHOWTIME_OFFSETS_MINUTES.in1h);
      const allSeats = await getShowtimeSeats(api, showtime.showtimeId);
      seats = [pickAdjacentSeats(allSeats, 1)[0]];
      const remaining = allSeats.filter((seat) => seat.showtimeSeatId !== seats[0].showtimeSeatId);
      seats.push(pickAdjacentSeats(remaining, 1)[0]);
    } finally {
      await api.dispose();
    }
  });

  test.afterAll(async ({ playwright }) => {
    if (!showtime) return;
    const api = await playwright.request.newContext();
    try {
      await cancelShowtime(api, adminToken, showtime.showtimeId);
    } catch {
      return;
    } finally {
      await api.dispose();
    }
  });

  test("F1: khách đưa thiếu tiền thì nút xác nhận bị khóa, backend cũng từ chối và đơn vẫn chờ thanh toán", async ({
    employeePage: page,
    request,
  }) => {
    test.setTimeout(UI_TEST_TIMEOUT_MS);
    const hold = await holdSeatAtCounter(page, seats[0]);
    const amountInput = page.getByLabel(AMOUNT_RECEIVED_LABEL, { exact: true });
    const confirmButton = page.getByRole("button", { name: CONFIRM_BUTTON_NAME, exact: true });
    const shortAmount = hold.finalAmount - SHORT_BY_AMOUNT;

    try {
      await test.step("Mặc định số tiền khách đưa bằng số tiền cần thanh toán và nút xác nhận mở", async () => {
        await expect(amountInput).toHaveValue(String(hold.finalAmount));
        await expect(confirmButton).toBeEnabled();
        await expect(page.getByText(INSUFFICIENT_UI_MESSAGE)).toHaveCount(0);
      });

      await test.step("Nhập thiếu tiền: báo chưa đủ, tiền thừa bằng 0, nút xác nhận bị khóa", async () => {
        await amountInput.fill(String(shortAmount));
        await expect(page.getByText(INSUFFICIENT_UI_MESSAGE)).toBeVisible();
        await expect(page.getByTestId("counter-change-amount")).toContainText(amountPattern(0));
        await expect(confirmButton).toBeDisabled();
      });

      await test.step("API: thanh toán thiếu tiền bị từ chối 422 với thông điệp số tiền không đủ", async () => {
        const attempt = await attemptCounterCheckout(request, employeeToken, hold.bookingId, shortAmount);
        expect(attempt.status).toBe(HTTP_UNPROCESSABLE);
        expect(attempt.message).toBe(INSUFFICIENT_API_MESSAGE);
      });

      await test.step("Đơn vẫn Pending, chưa có giao dịch và chưa có vé", async () => {
        const detail = await getAdminBookingDetail(request, employeeToken, hold.bookingId);
        expect(detail.status).toBe(BOOKING_STATUS.pending);
        expect(detail.payments).toHaveLength(0);
        expect(detail.tickets).toHaveLength(0);
      });
    } finally {
      await counterCancelViaApi(request, employeeToken, hold.bookingId);
    }
  });

  test("F2: khách đưa dư tiền thì hiển thị và trả đúng tiền thối, đơn chuyển sang đã xác nhận", async ({
    employeePage: page,
    request,
  }) => {
    test.setTimeout(UI_TEST_TIMEOUT_MS);
    const seat = seats[1];
    const hold = await holdSeatAtCounter(page, seat);
    const amountInput = page.getByLabel(AMOUNT_RECEIVED_LABEL, { exact: true });
    const confirmButton = page.getByRole("button", { name: CONFIRM_BUTTON_NAME, exact: true });
    const receivedAmount = hold.finalAmount + CHANGE_AMOUNT;

    await test.step("Nhập số tiền khách đưa lớn hơn: hiện đúng tiền thừa và không báo thiếu", async () => {
      await amountInput.fill(String(receivedAmount));
      await expect(page.getByTestId("counter-change-amount")).toContainText(amountPattern(CHANGE_AMOUNT));
      await expect(page.getByText(INSUFFICIENT_UI_MESSAGE)).toHaveCount(0);
      await expect(confirmButton).toBeEnabled();
    });

    const checkoutResponse = page.waitForResponse(
      (response) =>
        response.url().includes(`/admin/bookings/${hold.bookingId}/counter-checkout`) &&
        response.request().method() === "POST",
    );

    await test.step("Xác nhận thanh toán: màn hình kết quả báo thành công và đúng tiền thừa", async () => {
      await confirmButton.click();
      await expect(page.getByText(SUCCESS_HEADING, { exact: true })).toBeVisible({ timeout: PAGE_SETTLE_WAIT_MS });
      await expect(page.getByText(CHANGE_RESULT_PATTERN)).toContainText(amountPattern(CHANGE_AMOUNT));
    });

    await test.step("Backend trả đúng tiền thối", async () => {
      const checkout = ((await (await checkoutResponse).json()) as ApiEnvelope<CounterCheckout>).data;
      expect(checkout.bookingId).toBe(hold.bookingId);
      expect(checkout.change).toBe(CHANGE_AMOUNT);
    });

    await test.step("Đơn Confirmed, giao dịch tiền mặt Success đúng số tiền cần trả, có một vé chưa soát", async () => {
      const detail = await getAdminBookingDetail(request, employeeToken, hold.bookingId);
      expect(detail.status).toBe(BOOKING_STATUS.confirmed);
      expect(detail.payments).toHaveLength(1);
      expect(detail.payments[0].paymentMethod).toBe(CASH_METHOD);
      expect(detail.payments[0].status).toBe(PAYMENT_STATUS.success);
      expect(detail.payments[0].amount).toBe(hold.finalAmount);
      expect(detail.tickets).toHaveLength(1);
      expect(`${detail.tickets[0].seatRow}${detail.tickets[0].seatNumber}`).toBe(seatCode(seat));
      expect(detail.tickets[0].isCheckedIn).toBe(false);
    });
  });
});
