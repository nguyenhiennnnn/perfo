import { expect, test } from "@playwright/test";
import {
  AVAILABLE_SEAT_BUTTON,
  loginAsCustomer,
  openFirstBookableShowtime,
  getCustomerApiToken,
  getLatestPendingBookingId,
  expireBookingHoldInDb,
} from "./helpers";

test.setTimeout(180_000);

test("seat hold that expires mid-checkout notifies the customer and returns them to seat selection", async ({
  page,
}) => {
  await loginAsCustomer(page);

  await openFirstBookableShowtime(page);

  const availableSeats = page.locator(AVAILABLE_SEAT_BUTTON);
  await expect(availableSeats.first()).toBeVisible({ timeout: 10_000 });
  const seatCount = await availableSeats.count();
  test.skip(
    seatCount < 1,
    "No available seats on this showtime to run the scenario",
  );
  await availableSeats.first().click();

  // If clicking a single seat violates the cinema gap rule (lone seat in row),
  // canHold stays permanently false.  Pick a second seat to resolve it.
  const loneSeatWarning = page.getByText(/Không thể để trống ghế lẻ/i);
  if (await loneSeatWarning.isVisible({ timeout: 1_500 }).catch(() => false)) {
    // The clicked seat is no longer "available", so `.first()` is now the adjacent seat.
    if (seatCount >= 2) await availableSeats.first().click();
  }

  const continueButton = page.getByRole("button", {
    name: /Giữ ghế & tiếp tục thanh toán/i,
  });
  // Wait for auth hydration: canHold requires customerName populated from authUser.fullName.
  // Allow extra time since the booking page may be a cold lazy load.
  await expect(continueButton).toBeEnabled({ timeout: 30_000 });
  await continueButton.click();

  await expect(page.getByText(/Giữ ghế còn/i)).toBeVisible({ timeout: 20_000 });

  const token = await getCustomerApiToken(page);
  test.skip(!token, "Could not authenticate as customer via API");
  const bookingId = await getLatestPendingBookingId(page, token!);
  test.skip(
    !bookingId,
    "Could not resolve the pending booking created by the hold",
  );

  const forced = await expireBookingHoldInDb(bookingId!);
  test.skip(
    !forced,
    "Could not reach the database container to force-expire the hold",
  );

  await expect(
    page.getByText(/Thời gian giữ ghế đã hết hạn, vui lòng chọn lại\./i),
  ).toBeVisible({ timeout: 120_000 });

  await expect(continueButton).toBeVisible({ timeout: 10_000 });
  await expect(page.getByText(/Giữ ghế còn/i)).not.toBeVisible();
});
