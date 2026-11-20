import { expect, test } from "@playwright/test";
import {
  loginAsCustomer,
  getCustomerApiToken,
  holdFirstAvailableSeat,
  cancelBookingViaApi,
} from "./helpers";

test("customer sees a clear failure state when a booking ends up cancelled/failed, not a fake success", async ({
  page,
}) => {
  await loginAsCustomer(page);

  const token = await getCustomerApiToken(page);
  test.skip(!token, "Could not authenticate as customer via API");

  const hold = await holdFirstAvailableSeat(page, token!);
  test.skip(!hold, "No available seat on showtime 1 to run the scenario");

  const cancelled = await cancelBookingViaApi(page, token!, hold!.bookingId);
  test.skip(
    !cancelled,
    "Could not cancel the booking to simulate a payment failure",
  );

  await page.goto(`/booking/result?bookingId=${hold!.bookingId}`);

  await expect(
    page.getByRole("heading", { name: /Đơn đặt vé đã bị h[uủ][yỷ]/i }),
  ).toBeVisible({ timeout: 20_000 });
  await expect(
    page.getByText(/Giao dịch không thành công hoặc đã bị/i),
  ).toBeVisible();
  await expect(page.getByText(hold!.bookingCode)).toBeVisible();

  await expect(page.locator("main").getByText(/Vé điện tử/i)).not.toBeVisible();

  await expect(
    page.getByRole("button", { name: /Xem vé của tôi/i }).or(page.getByRole("link", { name: /Xem vé của tôi/i })),
  ).toBeVisible();
  await expect(
    page.getByRole("button", { name: /Về trang chủ/i }).or(page.getByRole("link", { name: /Về trang chủ/i })),
  ).toBeVisible();
});

test("visiting the booking result page for a non-existent booking shows a retry state, not a crash", async ({
  page,
}) => {
  await loginAsCustomer(page);

  await page.goto("/booking/result?bookingId=999999999");

  // The API answers 404 for an unknown booking, which the page renders as a "not found" state
  // with a way back to the customer's tickets (not a crash / blank page).
  await expect(page.getByText(/Không tìm thấy đơn đặt vé/i).first()).toBeVisible({
    timeout: 20_000,
  });
  await expect(
    page.getByRole("button", { name: /Xem vé của tôi/i }).or(page.getByRole("link", { name: /Xem vé của tôi/i })),
  ).toBeVisible();
});
