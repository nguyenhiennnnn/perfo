import { expect, test } from "@playwright/test";
import {
  AVAILABLE_SEAT_BUTTON,
  loginAsCustomer,
  openFirstBookableShowtime,
} from "./helpers";

test("customer end-to-end booking flow: select movie, pick seats, add combos, hold, check payment & release", async ({
  page,
}) => {
  test.setTimeout(120_000);
  // 1. Log in as Customer (also cancels any previous pending bookings)
  await loginAsCustomer(page);

  // 2-3. Browse the movie catalog / schedule, open an active bookable showtime
  await openFirstBookableShowtime(page);

  // 4. In seat selection step, select 2 available seats
  const availableSeats = page.locator(AVAILABLE_SEAT_BUTTON);
  await expect(availableSeats.first()).toBeVisible({ timeout: 15_000 });
  const seatCount = await availableSeats.count();
  test.skip(
    seatCount < 2,
    "Not enough available seats on this showtime to run the scenario",
  );

  // A selected seat stops matching AVAILABLE_SEAT_BUTTON (its label becomes "đang chọn"), so
  // `.first()` always points at the next free seat, which keeps the two seats adjacent.
  await availableSeats.first().click();
  await availableSeats.first().click();

  // If selecting two seats leaves a lone seat under the cinema gap rule, pick the adjacent seat
  const loneSeatWarning = page.getByText(/Không thể để trống ghế lẻ/i);
  const hasLoneSeat = await loneSeatWarning
    .waitFor({ state: "visible", timeout: 1_500 })
    .then(() => true)
    .catch(() => false);
  if (hasLoneSeat) {
    await availableSeats.first().click();
  }

  // Verify seat price summary is displayed
  await expect(page.getByText(/Chi tiết đơn hàng/i)).toBeVisible();
  await expect(page.getByText(/Ghế \(\d+\)/i)).toBeVisible();

  // 5. If combos are available, increase the first combo quantity
  const comboSection = page
    .locator("div", {
      has: page.getByRole("heading", { name: /Chọn combo/i }),
    })
    .last();
  const comboPlusButton = comboSection
    .locator('button:has(svg[class*="lucide-plus"])')
    .first();
  if (await comboPlusButton.isVisible()) {
    await comboPlusButton.click();
    // The price summary lists the chosen combo as "<quantity>x <name>"
    await expect(page.getByText(/^1x\s/).first()).toBeVisible();
  }

  // 6. Hold seats & continue to payment step
  const continueButton = page.getByRole("button", {
    name: /Giữ ghế & tiếp tục thanh toán/i,
  });
  // Wait for auth hydration: canHold requires customerName populated from authUser.fullName
  await expect(continueButton).toBeEnabled({ timeout: 20_000 });
  await continueButton.click();

  // 7. Payment step assertions:
  // Check hold countdown timer is active (hold API may take a moment to respond)
  await expect(page.getByText(/Giữ ghế còn/i)).toBeVisible({ timeout: 20_000 });

  // Verify payment action button is visible
  const paymentButton = page.getByRole("button", { name: /^Thanh toán\s/i });
  await expect(paymentButton).toBeVisible({ timeout: 10_000 });

  // Verify payment methods can be switched
  const momoOption = page.getByRole("radio", { name: "MoMo" });
  if (await momoOption.isVisible()) {
    await momoOption.click();
    await expect(momoOption).toHaveAttribute("aria-checked", "true");
  }

  const vnpayOption = page.getByRole("radio", { name: "VNPay" });
  if (await vnpayOption.isVisible()) {
    await vnpayOption.click();
    await expect(vnpayOption).toHaveAttribute("aria-checked", "true");
  }

  // 8. Cancel hold to release seats back to the showtime
  const cancelButton = page.getByRole("button", { name: /Hủy, chọn lại ghế/i });
  await expect(cancelButton).toBeVisible();
  await cancelButton.click();

  // Verify returned cleanly to seat selection view
  await expect(continueButton).toBeVisible({ timeout: 10_000 });

  // 9. Navigate to My Tickets to verify account ticket history is accessible
  await page.goto("/account/tickets");
  await expect(page.getByRole("heading", { name: /Vé của tôi/i })).toBeVisible({
    timeout: 20_000,
  });
  await expect(page.getByRole("tab", { name: "Tất cả" })).toBeVisible();
});
