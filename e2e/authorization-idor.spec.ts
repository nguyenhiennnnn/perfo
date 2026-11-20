import { expect, test } from "./fixtures";
import { callApi } from "./helpers/api-call";
import { getAccessToken } from "./helpers/auth";
import {
  BOOKING_STATUS,
  cancelBookingViaApi,
  checkoutViaApi,
  getBookingDetail,
  holdSeatsViaApi,
  PAYMENT_STATUS,
  type HoldResult,
} from "./helpers/booking-api";
import {
  createConfirmedBooking,
  pickSeatsInDistinctRows,
  type ConfirmedBooking,
} from "./helpers/booking-confirmed";
import { getShowtimeSeats } from "./helpers/booking-flow";
import {
  cancelShowtime,
  createShowtimeIn,
  SHOWTIME_OFFSETS_MINUTES,
  type CreatedShowtime,
} from "./helpers/showtimes";

/**
 * Test #10: phân quyền theo vai trò và IDOR (khách này đụng vào đơn của khách khác).
 *
 * Quy ước mã (theo hành vi backend hiện tại, ForbiddenAppException = 403 khi không phải chủ đơn):
 *  - I1: đọc chi tiết đơn của người khác;
 *  - I2: tải PDF vé của người khác;
 *  - I4: hủy đơn của người khác;
 *  - I6: thanh toán (checkout) đơn của người khác.
 * Mỗi ca đều có đối chứng "chủ đơn thì được" để chắc chắn 403 đến từ phân quyền chứ không phải vì trạng thái đơn.
 */

const SETUP_TIMEOUT_MS = 120_000;
const UI_TEST_TIMEOUT_MS = 60_000;
const HTTP_OK = 200;
const HTTP_UNAUTHORIZED = 401;
const HTTP_FORBIDDEN = 403;
const PDF_SIGNATURE = "%PDF";

const LOAD_ERROR_TITLE = "Không tải được đơn đặt vé";
const LOAD_ERROR_FORBIDDEN_CAUSE = "Tài khoản hiện tại không đủ quyền xem dữ liệu này.";
const DASHBOARD_HEADING = "Tổng quan";

/** API chỉ dành cho Admin (Policies.AdminOnly): nhân viên và khách đều phải bị từ chối. */
const ADMIN_ONLY_PATHS = [
  "/admin/dashboard/summary",
  "/admin/users",
  "/admin/employees",
  "/admin/settings",
] as const;

/** API dành cho nhân viên và admin (Policies.StaffOnly): khách phải bị từ chối. */
const STAFF_PATHS = ["/admin/bookings", "/admin/users/customers"] as const;

interface Tokens {
  admin: string;
  /** Khách A: chủ của các đơn trong bộ test. */
  customer: string;
  /** Khách B: người lạ, cố đụng vào đơn của A. */
  customer2: string;
  employee: string;
}

let tokens: Tokens;
let showtime: CreatedShowtime | undefined;
/** Đơn Confirmed của khách A (có vé, có thanh toán thành công). */
let confirmed: ConfirmedBooking;
/** Đơn Pending (đang giữ ghế) của khách A, chưa thanh toán. */
let pending: HoldResult;

test.beforeAll(async ({ playwright }) => {
  test.setTimeout(SETUP_TIMEOUT_MS);
  const api = await playwright.request.newContext();
  try {
    // Đăng nhập một lần cho cả file: endpoint auth bị rate limit 5 lần / 60 giây theo IP.
    tokens = {
      admin: await getAccessToken(api, "admin"),
      customer: await getAccessToken(api, "customer"),
      customer2: await getAccessToken(api, "customer2"),
      employee: await getAccessToken(api, "employee"),
    };

    showtime = await createShowtimeIn(api, tokens.admin, SHOWTIME_OFFSETS_MINUTES.in3h);
    const [confirmedSeat, pendingSeat] = pickSeatsInDistinctRows(
      await getShowtimeSeats(api, showtime.showtimeId),
      2,
    );

    confirmed = await createConfirmedBooking(
      api,
      tokens.customer,
      showtime.showtimeId,
      confirmedSeat,
    );
    pending = await holdSeatsViaApi(api, tokens.customer, {
      showtimeId: showtime.showtimeId,
      seatIds: [pendingSeat.showtimeSeatId],
    });
  } finally {
    await api.dispose();
  }
});

test.afterAll(async ({ playwright }) => {
  if (!tokens) return;
  const api = await playwright.request.newContext();
  try {
    if (pending) await cancelBookingViaApi(api, tokens.customer, pending.bookingId);
    if (showtime) {
      try {
        await cancelShowtime(api, tokens.admin, showtime.showtimeId);
      } catch {
        // Suất đã có đơn xác nhận nên backend có thể từ chối hủy; DB E2E riêng nên bỏ qua.
      }
    }
  } finally {
    await api.dispose();
  }
});

test.describe("Khách không đụng được vào đơn của người khác (I1, I2, I4, I6)", () => {
  test("I1: khách B đọc chi tiết đơn của khách A bị từ chối 403 và không lộ dữ liệu đơn", async ({
    request,
  }) => {
    const owner = await callApi(request, {
      path: `/bookings/${confirmed.bookingId}`,
      token: tokens.customer,
    });
    expect(owner.status).toBe(HTTP_OK);
    expect(owner.text).toContain(confirmed.bookingCode);

    const stranger = await callApi(request, {
      path: `/bookings/${confirmed.bookingId}`,
      token: tokens.customer2,
    });
    expect(stranger.status).toBe(HTTP_FORBIDDEN);
    expect(stranger.json?.success).toBe(false);
    expect(stranger.text).not.toContain(confirmed.bookingCode);

    // Danh sách "đơn của tôi" của khách B cũng không chứa đơn của khách A.
    const list = await callApi<{ items: { bookingId: number }[] }>(request, {
      path: "/bookings",
      token: tokens.customer2,
    });
    expect(list.status).toBe(HTTP_OK);
    const listedIds = (list.json?.data?.items ?? []).map((item) => item.bookingId);
    expect(listedIds).not.toContain(confirmed.bookingId);
    expect(listedIds).not.toContain(pending.bookingId);
  });

  test("I1: khách B mở trang chi tiết vé của khách A thì chỉ thấy báo lỗi quyền, không thấy mã đơn", async ({
    customer2Page: page,
  }) => {
    test.setTimeout(UI_TEST_TIMEOUT_MS);
    await page.goto(`/account/tickets/${confirmed.bookingId}`);

    await expect(page.getByRole("heading", { name: LOAD_ERROR_TITLE })).toBeVisible();
    await expect(page.getByText(LOAD_ERROR_FORBIDDEN_CAUSE)).toBeVisible();
    await expect(page.getByTestId("ticket-detail-status")).toHaveCount(0);
    await expect(page.getByText(confirmed.bookingCode)).toHaveCount(0);
  });

  test("I2: khách B tải PDF vé của khách A bị từ chối 403, trong khi khách A tải được", async ({
    request,
  }) => {
    const owner = await callApi(request, {
      path: `/bookings/${confirmed.bookingId}/tickets/pdf`,
      token: tokens.customer,
    });
    expect(owner.status).toBe(HTTP_OK);
    expect(owner.contentType).toContain("application/pdf");
    expect(owner.buffer.subarray(0, PDF_SIGNATURE.length).toString("latin1")).toBe(PDF_SIGNATURE);

    const stranger = await callApi(request, {
      path: `/bookings/${confirmed.bookingId}/tickets/pdf`,
      token: tokens.customer2,
    });
    expect(stranger.status).toBe(HTTP_FORBIDDEN);
    expect(stranger.contentType).not.toContain("application/pdf");
    expect(stranger.buffer.subarray(0, PDF_SIGNATURE.length).toString("latin1")).not.toBe(
      PDF_SIGNATURE,
    );
  });

  test("I4: khách B hủy đơn đã thanh toán của khách A bị từ chối 403, đơn vẫn Confirmed và không bị hoàn tiền", async ({
    request,
  }) => {
    const attempt = await callApi(request, {
      method: "POST",
      path: `/bookings/${confirmed.bookingId}/cancel`,
      token: tokens.customer2,
    });
    expect(attempt.status).toBe(HTTP_FORBIDDEN);

    const detail = await getBookingDetail(request, tokens.customer, confirmed.bookingId);
    expect(detail.status).toBe(BOOKING_STATUS.confirmed);
    expect(detail.tickets).toHaveLength(1);
    expect(detail.payments[0]?.status).toBe(PAYMENT_STATUS.success);
    expect(detail.payments[0]?.refundedAmount ?? 0).toBe(0);
  });

  test("I4: khách B hủy đơn đang giữ ghế của khách A bị từ chối 403, ghế vẫn được giữ cho khách A", async ({
    request,
  }) => {
    const attempt = await callApi(request, {
      method: "POST",
      path: `/bookings/${pending.bookingId}/cancel`,
      token: tokens.customer2,
    });
    expect(attempt.status).toBe(HTTP_FORBIDDEN);

    const detail = await getBookingDetail(request, tokens.customer, pending.bookingId);
    expect(detail.status).toBe(BOOKING_STATUS.pending);
    expect(detail.holdExpiresAt).toBeTruthy();
  });

  test("I6: khách B thanh toán đơn đang giữ ghế của khách A bị từ chối 403 và không tạo giao dịch", async ({
    request,
  }) => {
    const attempt = await callApi(request, {
      method: "POST",
      path: `/bookings/${pending.bookingId}/checkout`,
      token: tokens.customer2,
      data: { paymentMethod: "VNPay" },
    });
    expect(attempt.status).toBe(HTTP_FORBIDDEN);
    expect(attempt.text).not.toContain("vnp_TxnRef");

    const detail = await getBookingDetail(request, tokens.customer, pending.bookingId);
    expect(detail.status).toBe(BOOKING_STATUS.pending);
    expect(detail.payments).toHaveLength(0);

    // Đối chứng: chính khách A thanh toán cùng đơn này thì được.
    const ownerPayment = await checkoutViaApi(request, tokens.customer, pending.bookingId);
    expect(ownerPayment.txnRef).toBeTruthy();
  });
});

test.describe("Phân quyền theo vai trò", () => {
  test("chưa đăng nhập thì mọi API khách và API quản trị đều trả 401", async ({ request }) => {
    const paths = [
      "/bookings",
      `/bookings/${confirmed.bookingId}`,
      `/bookings/${confirmed.bookingId}/tickets/pdf`,
      "/users/me",
      ...STAFF_PATHS,
      ...ADMIN_ONLY_PATHS,
    ];

    for (const path of paths) {
      const response = await callApi(request, { path });
      expect(response.status, `GET ${path} khi chưa đăng nhập`).toBe(HTTP_UNAUTHORIZED);
      expect(response.text).not.toContain(confirmed.bookingCode);
    }
  });

  test("khách hàng bị từ chối 403 ở mọi API quản trị, kể cả API đọc đơn và PDF vé theo mã đơn", async ({
    request,
  }) => {
    const paths = [
      ...STAFF_PATHS,
      ...ADMIN_ONLY_PATHS,
      `/admin/bookings/${confirmed.bookingId}`,
      `/admin/bookings/${confirmed.bookingId}/tickets/pdf`,
    ];

    // Khách B (người lạ) và khách A (chủ đơn) đều không được vào API quản trị.
    for (const [owner, token] of [
      ["khách A (chủ đơn)", tokens.customer],
      ["khách B", tokens.customer2],
    ] as const) {
      for (const path of paths) {
        const response = await callApi(request, { path, token });
        expect(response.status, `${owner} gọi GET ${path}`).toBe(HTTP_FORBIDDEN);
        expect(response.text).not.toContain(confirmed.bookingCode);
      }
    }
  });

  test("nhân viên dùng được API nghiệp vụ nhưng bị từ chối 403 ở API chỉ dành cho quản trị, admin thì được tất cả", async ({
    request,
  }) => {
    for (const path of STAFF_PATHS) {
      const response = await callApi(request, { path, token: tokens.employee });
      expect(response.status, `nhân viên gọi GET ${path}`).toBe(HTTP_OK);
    }

    for (const path of ADMIN_ONLY_PATHS) {
      const denied = await callApi(request, { path, token: tokens.employee });
      expect(denied.status, `nhân viên gọi GET ${path}`).toBe(HTTP_FORBIDDEN);

      // Đối chứng: cùng đường dẫn, admin gọi thì thành công (403 ở trên là do vai trò).
      const allowed = await callApi(request, { path, token: tokens.admin });
      expect(allowed.status, `admin gọi GET ${path}`).toBe(HTTP_OK);
    }
  });

  test("khách hàng mở trang quản trị thì bị đưa về trang chủ và không thấy nội dung quản trị", async ({
    customerPage: page,
  }) => {
    test.setTimeout(UI_TEST_TIMEOUT_MS);
    await page.goto("/admin/dashboard");

    await expect(page).toHaveURL(/\/$/);
    await expect(page.getByRole("heading", { name: DASHBOARD_HEADING, level: 1 })).toHaveCount(0);
    await expect(page.getByTestId("header-user-menu")).toBeVisible();
  });

  test("nhân viên mở trang chỉ dành cho quản trị thì bị đưa về trang bán vé tại quầy", async ({
    employeePage: page,
  }) => {
    test.setTimeout(UI_TEST_TIMEOUT_MS);

    for (const path of ["/admin/dashboard", "/admin/employees"]) {
      await page.goto(path);
      await expect(page, `nhân viên mở ${path}`).toHaveURL(/\/admin\/bookings\/counter$/);
    }
    await expect(page.getByRole("heading", { name: DASHBOARD_HEADING, level: 1 })).toHaveCount(0);
  });

  test("người chưa đăng nhập mở trang vé của khách hoặc trang quản trị thì bị chuyển tới trang đăng nhập", async ({
    page,
  }) => {
    test.setTimeout(UI_TEST_TIMEOUT_MS);

    await page.goto(`/account/tickets/${confirmed.bookingId}`);
    await expect(page).toHaveURL(
      new RegExp(`/auth/login\\?redirect=${encodeURIComponent(`/account/tickets/${confirmed.bookingId}`)}$`),
    );
    await expect(page.getByRole("heading", { name: "Đăng nhập", level: 1 })).toBeVisible();
    await expect(page.getByText(confirmed.bookingCode)).toHaveCount(0);

    await page.goto("/admin/dashboard");
    await expect(page).toHaveURL(/\/admin\/login$/);
  });
});
