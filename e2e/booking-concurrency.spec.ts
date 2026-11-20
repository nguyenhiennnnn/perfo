import type { Page, PlaywrightWorkerArgs } from "@playwright/test";
import { expect, test } from "./fixtures";
import { getAccessToken } from "./helpers/auth";
import {
  getSeatStatus,
  getShowtimeSeats,
  interceptVnPayRedirect,
  pickAdjacentSeats,
  SEAT_STATUS,
  seatCode,
  type ShowtimeSeat,
} from "./helpers/booking-flow";
import { sendIpn, signIpn } from "./helpers/ipn";
import { trackShowtimeHub } from "./helpers/realtime";
import {
  cancelShowtime,
  createShowtimeIn,
  SHOWTIME_OFFSETS_MINUTES,
  type CreatedShowtime,
} from "./helpers/showtimes";

const HOLD_BUTTON_NAME = "Giữ ghế & tiếp tục thanh toán";
const CANCEL_HOLD_BUTTON_NAME = "Hủy, chọn lại ghế";
const PAY_BUTTON_NAME = /^Thanh toán /;
const SEAT_TAKEN_ERROR = /đã bị giữ\/đặt bởi người khác/;
const IPN_SUCCESS_CODE = "00";
const NO_RELOAD_MARKER = "__e2eNoReload";

const SETUP_TIMEOUT_MS = 120_000;
const CONTENTION_TEST_TIMEOUT_MS = 90_000;
const REALTIME_TEST_TIMEOUT_MS = 120_000;
const PAGE_SETTLE_WAIT_MS = 15_000;
const REALTIME_PROPAGATION_WAIT_MS = 10_000;
const SERVER_STATE_WAIT_MS = 15_000;

interface PreparedShowtime {
  showtime: CreatedShowtime;
  seat: ShowtimeSeat;
}

const seatButton = (page: Page, seat: ShowtimeSeat, state: string) =>
  page.getByRole("button", { name: `Ghế ${seatCode(seat)}, ${state}`, exact: true });

const holdButton = (page: Page) => page.getByRole("button", { name: HOLD_BUTTON_NAME, exact: true });

/** Tạo suất chiếu +3 giờ (phòng riêng cho từng suất) và chọn một ghế giữa hàng còn trống. */
async function prepareShowtimes(
  playwright: PlaywrightWorkerArgs["playwright"],
  count: number,
): Promise<{ prepared: PreparedShowtime[]; adminToken: string }> {
  const api = await playwright.request.newContext();
  try {
    const adminToken = await getAccessToken(api, "admin");
    const prepared: PreparedShowtime[] = [];
    const usedRoomIds: number[] = [];
    for (let i = 0; i < count; i++) {
      const showtime = await createShowtimeIn(api, adminToken, SHOWTIME_OFFSETS_MINUTES.in3h, {
        excludeRoomIds: usedRoomIds,
      });
      usedRoomIds.push(showtime.roomId);
      const [seat] = pickAdjacentSeats(await getShowtimeSeats(api, showtime.showtimeId), 1);
      prepared.push({ showtime, seat });
    }
    return { prepared, adminToken };
  } finally {
    await api.dispose();
  }
}

async function cleanupShowtimes(
  playwright: PlaywrightWorkerArgs["playwright"],
  adminToken: string | undefined,
  prepared: PreparedShowtime[],
): Promise<void> {
  if (!adminToken) return;
  const api = await playwright.request.newContext();
  try {
    for (const { showtime } of prepared) {
      try {
        await cancelShowtime(api, adminToken, showtime.showtimeId);
      } catch {
        // Suất đã có đơn giữ/đặt nên backend có thể từ chối hủy; DB E2E riêng nên bỏ qua.
      }
    }
  } finally {
    await api.dispose();
  }
}

async function openBookingPage(page: Page, { showtime, seat }: PreparedShowtime) {
  await page.goto(`/booking/${showtime.showtimeId}`);
  await expect(seatButton(page, seat, "trống")).toBeVisible({ timeout: PAGE_SETTLE_WAIT_MS });
}

async function selectSeat(page: Page, seat: ShowtimeSeat) {
  await seatButton(page, seat, "trống").click();
  await expect(seatButton(page, seat, "đang chọn")).toHaveAttribute("aria-pressed", "true");
}

test.describe("Hai khách tranh cùng ghế (C2)", () => {
  let showtimes: PreparedShowtime[] = [];
  let adminToken: string | undefined;

  test.beforeAll(async ({ playwright }) => {
    test.setTimeout(SETUP_TIMEOUT_MS);
    const result = await prepareShowtimes(playwright, 2);
    showtimes = result.prepared;
    adminToken = result.adminToken;
  });

  test.afterAll(async ({ playwright }) => {
    await cleanupShowtimes(playwright, adminToken, showtimes);
  });

  test("khách đến sau bị từ chối khi ghế đã được khách trước giữ", async ({
    customerPage: pageA,
    customer2Page: pageB,
    request,
  }) => {
    test.setTimeout(CONTENTION_TEST_TIMEOUT_MS);
    const target = showtimes[0];

    await test.step("Cả hai khách mở suất chiếu và cùng chọn một ghế", async () => {
      await Promise.all([openBookingPage(pageA, target), openBookingPage(pageB, target)]);
      await Promise.all([selectSeat(pageA, target.seat), selectSeat(pageB, target.seat)]);
    });

    await test.step("Khách A giữ ghế thành công", async () => {
      await holdButton(pageA).click();
      await expect(pageA.getByRole("timer")).toBeVisible({ timeout: PAGE_SETTLE_WAIT_MS });
    });

    await test.step("Khách B bấm giữ cùng ghế thì bị báo ghế đã có người giữ", async () => {
      await expect(holdButton(pageB)).toBeEnabled();
      await holdButton(pageB).click();
      await expect(pageB.getByText(SEAT_TAKEN_ERROR).first()).toBeVisible({ timeout: PAGE_SETTLE_WAIT_MS });
      await expect(holdButton(pageB)).toBeVisible();
      await expect(pageB.getByRole("timer")).toHaveCount(0);
    });

    await test.step("Ghế vẫn do khách A giữ", async () => {
      await expect(pageA.getByRole("timer")).toBeVisible();
      await expect
        .poll(() => getSeatStatus(request, target.showtime.showtimeId, target.seat.showtimeSeatId), {
          message: "ghế tranh chấp phải ở trạng thái Held",
          timeout: SERVER_STATE_WAIT_MS,
        })
        .toBe(SEAT_STATUS.held);
    });
  });

  test("hai khách bấm giữ cùng lúc thì đúng một người giữ được", async ({
    customerPage: pageA,
    customer2Page: pageB,
    request,
  }) => {
    test.setTimeout(CONTENTION_TEST_TIMEOUT_MS);
    const target = showtimes[1];
    const timersShown = async () =>
      (await pageA.getByRole("timer").count()) + (await pageB.getByRole("timer").count());

    await test.step("Cả hai khách mở suất chiếu và cùng chọn một ghế", async () => {
      await Promise.all([openBookingPage(pageA, target), openBookingPage(pageB, target)]);
      await Promise.all([selectSeat(pageA, target.seat), selectSeat(pageB, target.seat)]);
    });

    await test.step("Hai khách bấm giữ ghế đồng thời", async () => {
      await Promise.all([holdButton(pageA).click(), holdButton(pageB).click()]);
    });

    await test.step("Đúng một khách sang bước thanh toán, khách còn lại nhận lỗi tranh ghế", async () => {
      await expect
        .poll(timersShown, { message: "đúng một trang được vào bước thanh toán", timeout: PAGE_SETTLE_WAIT_MS })
        .toBe(1);

      const loser = (await pageA.getByRole("timer").count()) === 0 ? pageA : pageB;
      await expect(loser.getByText(SEAT_TAKEN_ERROR).first()).toBeVisible({ timeout: PAGE_SETTLE_WAIT_MS });
      await expect(holdButton(loser)).toBeVisible();
      expect(await timersShown()).toBe(1);
    });

    await test.step("Ghế chỉ bị giữ một lần trên backend", async () => {
      await expect
        .poll(() => getSeatStatus(request, target.showtime.showtimeId, target.seat.showtimeSeatId), {
          message: "ghế tranh chấp phải ở trạng thái Held",
          timeout: SERVER_STATE_WAIT_MS,
        })
        .toBe(SEAT_STATUS.held);
    });
  });
});

test.describe("Realtime ghế qua SignalR (C3, C4)", () => {
  let showtimes: PreparedShowtime[] = [];
  let adminToken: string | undefined;

  test.beforeAll(async ({ playwright }) => {
    test.setTimeout(SETUP_TIMEOUT_MS);
    const result = await prepareShowtimes(playwright, 2);
    showtimes = result.prepared;
    adminToken = result.adminToken;
  });

  test.afterAll(async ({ playwright }) => {
    await cleanupShowtimes(playwright, adminToken, showtimes);
  });

  test("khách B thấy ghế đổi sang đang giữ rồi trống lại mà không tải lại trang khi khách A giữ rồi hủy", async ({
    customerPage: pageA,
    customer2Page: pageB,
  }) => {
    test.setTimeout(REALTIME_TEST_TIMEOUT_MS);
    const target = showtimes[0];
    const hubOfB = trackShowtimeHub(pageB);

    await test.step("Khách B mở suất chiếu và đã vào nhóm realtime", async () => {
      await openBookingPage(pageB, target);
      await hubOfB.waitUntilJoined();
      await pageB.evaluate((marker) => {
        (window as unknown as Record<string, boolean>)[marker] = true;
      }, NO_RELOAD_MARKER);
    });

    await test.step("Khách A chọn ghế và giữ ghế", async () => {
      await openBookingPage(pageA, target);
      await selectSeat(pageA, target.seat);
      await holdButton(pageA).click();
      await expect(pageA.getByRole("timer")).toBeVisible({ timeout: PAGE_SETTLE_WAIT_MS });
    });

    await test.step("C3: trang khách B tự cập nhật ghế sang đang giữ", async () => {
      await expect(seatButton(pageB, target.seat, "đang giữ")).toBeVisible({
        timeout: REALTIME_PROPAGATION_WAIT_MS,
      });
      await expect(seatButton(pageB, target.seat, "đang giữ")).toBeDisabled();
    });

    await test.step("Khách A hủy giữ ghế", async () => {
      await pageA.getByRole("button", { name: CANCEL_HOLD_BUTTON_NAME, exact: true }).click();
      await expect(holdButton(pageA)).toBeVisible({ timeout: PAGE_SETTLE_WAIT_MS });
    });

    await test.step("C4: trang khách B tự cập nhật ghế trở lại trống", async () => {
      await expect(seatButton(pageB, target.seat, "trống")).toBeVisible({
        timeout: REALTIME_PROPAGATION_WAIT_MS,
      });
      await expect(seatButton(pageB, target.seat, "trống")).toBeEnabled();
    });

    await test.step("Trang khách B chưa từng được tải lại", async () => {
      const kept = await pageB.evaluate(
        (marker) => (window as unknown as Record<string, boolean>)[marker] === true,
        NO_RELOAD_MARKER,
      );
      expect(kept).toBe(true);
    });
  });

  test("khách B thấy ghế chuyển sang đã bán khi khách A thanh toán thành công", async ({
    customerPage: pageA,
    customer2Page: pageB,
    request,
  }) => {
    test.setTimeout(REALTIME_TEST_TIMEOUT_MS);
    const target = showtimes[1];
    const hubOfB = trackShowtimeHub(pageB);

    await test.step("Khách B mở suất chiếu và đã vào nhóm realtime", async () => {
      await openBookingPage(pageB, target);
      await hubOfB.waitUntilJoined();
    });

    const redirectPromise = await interceptVnPayRedirect(pageA);
    await test.step("Khách A giữ ghế và bấm thanh toán", async () => {
      await openBookingPage(pageA, target);
      await selectSeat(pageA, target.seat);
      await holdButton(pageA).click();
      await pageA.getByRole("button", { name: PAY_BUTTON_NAME }).click();
    });
    const redirect = await redirectPromise;

    await test.step("VNPay gọi IPN thành công về backend", async () => {
      const response = await sendIpn(request, signIpn({ txnRef: redirect.txnRef, amount: redirect.amount }));
      expect(response.rspCode).toBe(IPN_SUCCESS_CODE);
    });

    await test.step("Trang khách B tự cập nhật ghế sang đã bán", async () => {
      await expect(seatButton(pageB, target.seat, "đã bán")).toBeVisible({
        timeout: REALTIME_PROPAGATION_WAIT_MS,
      });
    });
  });
});
