import type { Page } from "@playwright/test";
import { expect, test } from "./fixtures";
import { callApi } from "./helpers/api-call";
import { ACCOUNTS, getAccessToken } from "./helpers/auth";
import {
  acquireOutbox,
  createEmployeeWithTemporaryPassword,
  deactivateAccounts,
  loginRequest,
  registerConfirmedCustomer,
  uniqueE2eEmail,
  waitForEmailToken,
  type ProvisionedAccount,
  type ProvisionedEmployee,
} from "./helpers/auth-accounts";
import type { MockGateway } from "./helpers/mock-gateway";

/**
 * Test #12: đăng nhập (A3), reset token dùng một lần (A6) và buộc đổi mật khẩu (A8).
 *
 * Token reset lấy từ OUTBOX EMAIL GIẢ LẬP (mock gateway, xem ./helpers/auth-accounts): backend gửi thư qua
 * Resend tới mock thay vì ra Internet, test đọc thư và tách token khỏi link y như người dùng bấm vào.
 * Mỗi lần chạy tạo tài khoản riêng (email ngẫu nhiên) nên không đổi mật khẩu của tài khoản seed.
 */

const SETUP_TIMEOUT_MS = 420_000;
const CLEANUP_TIMEOUT_MS = 60_000;
const UI_TEST_TIMEOUT_MS = 180_000;
const HTTP_OK = 200;
const HTTP_BAD_REQUEST = 400;
const HTTP_UNAUTHORIZED = 401;
const HTTP_FORBIDDEN = 403;

const NEW_PASSWORD = "Moi@123456";
const OTHER_PASSWORD = "Khac@123456";
const TEMPORARY_PASSWORD = "Tam@123456";
const WRONG_PASSWORD = "Sai@123456";

const INVALID_CREDENTIALS_MESSAGE = "Sai email hoặc mật khẩu.";
const ACCOUNT_LOCKED_MESSAGE = "Tài khoản đã bị khóa.";
const TOKEN_REJECTED_MESSAGE = "Token hết hạn, đã dùng hoặc không hợp lệ.";
const PASSWORD_CHANGE_REQUIRED_CODE = "PASSWORD_CHANGE_REQUIRED";
const FORGOT_PASSWORD_SENT_HEADING = "Đã gửi yêu cầu";

let mock: MockGateway | undefined;
let adminToken: string | undefined;
/** Khách dùng cho các ca đăng nhập (A3). */
let loginCustomer: ProvisionedAccount;
/** Khách dùng cho ca reset mật khẩu (A6): mật khẩu bị đổi nên không dùng chung với A3. */
let resetCustomer: ProvisionedAccount;
/** Nhân viên mật khẩu tạm cho ca kiểm tra bằng API (A8). */
let employeeApi: ProvisionedEmployee;
/** Nhân viên mật khẩu tạm cho ca kiểm tra bằng giao diện (A8). */
let employeeUi: ProvisionedEmployee;

test.beforeAll(async ({ playwright }) => {
  test.setTimeout(SETUP_TIMEOUT_MS);
  mock = await acquireOutbox();

  const api = await playwright.request.newContext();
  try {
    // Đăng nhập admin một lần cho cả file: endpoint auth bị rate limit 5 lần / 60 giây theo IP.
    adminToken = await getAccessToken(api, "admin");

    loginCustomer = await registerConfirmedCustomer(api, mock, uniqueE2eEmail("e2e.login"));
    resetCustomer = await registerConfirmedCustomer(api, mock, uniqueE2eEmail("e2e.reset"));
    employeeApi = await createEmployeeWithTemporaryPassword(
      api,
      adminToken,
      uniqueE2eEmail("e2e.nv.api"),
      TEMPORARY_PASSWORD,
    );
    employeeUi = await createEmployeeWithTemporaryPassword(
      api,
      adminToken,
      uniqueE2eEmail("e2e.nv.ui"),
      TEMPORARY_PASSWORD,
    );
  } finally {
    await api.dispose();
  }
});

test.afterAll(async ({ playwright }) => {
  test.setTimeout(CLEANUP_TIMEOUT_MS);
  try {
    if (adminToken) {
      const api = await playwright.request.newContext();
      try {
        await deactivateAccounts(api, adminToken, {
          customers: [loginCustomer, resetCustomer].filter(Boolean),
          employees: [employeeApi, employeeUi].filter(Boolean),
        });
      } finally {
        await api.dispose();
      }
    }
  } finally {
    await mock?.close();
  }
});

async function loginViaCustomerForm(page: Page, email: string, password: string) {
  await page.goto("/auth/login");
  await page.getByLabel("Email", { exact: true }).fill(email);
  await page.getByLabel("Mật khẩu", { exact: true }).fill(password);
  await page.getByRole("button", { name: "Đăng nhập", exact: true }).click();
}

async function loginViaAdminForm(page: Page, email: string, password: string) {
  await page.goto("/admin/login");
  await page.getByLabel("Email quản trị", { exact: true }).fill(email);
  await page.getByLabel("Mật khẩu", { exact: true }).fill(password);
  await page.getByRole("button", { name: "Đăng nhập quản trị", exact: true }).click();
}

test.describe("Đăng nhập (A3)", () => {
  test("A3: khách nhập đúng email và mật khẩu thì vào trang chủ với menu người dùng", async ({
    page,
  }) => {
    await loginViaCustomerForm(page, loginCustomer.email, loginCustomer.password);

    await expect(page).toHaveURL(/\/$/);
    await expect(page.getByTestId("header-user-menu")).toBeVisible();
    await expect(page.getByRole("link", { name: "Đăng nhập" })).toHaveCount(0);
  });

  test("A3: sai mật khẩu hoặc email không tồn tại cùng nhận một thông báo chung, không đăng nhập được", async ({
    page,
    request,
  }) => {
    await loginViaCustomerForm(page, loginCustomer.email, WRONG_PASSWORD);

    await expect(page.getByTestId("form-error-alert")).toHaveText(INVALID_CREDENTIALS_MESSAGE);
    await expect(page).toHaveURL(/\/auth\/login$/);
    await expect(page.getByTestId("header-user-menu")).toHaveCount(0);

    // Backend không phân biệt "sai mật khẩu" với "email không tồn tại" (không lộ email nào đã đăng ký).
    const wrongPassword = await loginRequest(request, "customer", loginCustomer.email, WRONG_PASSWORD);
    const unknownEmail = await loginRequest(
      request,
      "customer",
      uniqueE2eEmail("e2e.khong-ton-tai"),
      WRONG_PASSWORD,
    );
    for (const response of [wrongPassword, unknownEmail]) {
      expect(response.status).toBe(HTTP_UNAUTHORIZED);
      expect(response.json?.message).toBe(INVALID_CREDENTIALS_MESSAGE);
      expect(response.text).not.toContain("accessToken");
    }
  });

  test("A3: cổng đăng nhập của khách và cổng quản trị tách biệt, tài khoản đăng nhập nhầm cổng bị từ chối", async ({
    request,
  }) => {
    const customerAtAdminPortal = await loginRequest(
      request,
      "admin",
      loginCustomer.email,
      loginCustomer.password,
    );
    expect(customerAtAdminPortal.status).toBe(HTTP_FORBIDDEN);
    expect(customerAtAdminPortal.text).not.toContain("accessToken");

    const { email, password } = ACCOUNTS.employee;
    const employeeAtCustomerPortal = await loginRequest(request, "customer", email, password);
    expect(employeeAtCustomerPortal.status).toBe(HTTP_FORBIDDEN);
    expect(employeeAtCustomerPortal.text).not.toContain("accessToken");
  });

  test("A3: tài khoản bị khóa không đăng nhập được dù đúng mật khẩu, mở khóa thì đăng nhập lại được", async ({
    request,
  }) => {
    const setActive = (isActive: boolean) =>
      callApi(request, {
        method: "PUT",
        path: `/admin/users/${loginCustomer.userId}/status`,
        token: adminToken,
        data: { isActive },
      });

    expect((await setActive(false)).status).toBe(HTTP_OK);
    try {
      const locked = await loginRequest(
        request,
        "customer",
        loginCustomer.email,
        loginCustomer.password,
      );
      expect(locked.status).toBe(HTTP_FORBIDDEN);
      expect(locked.json?.message).toBe(ACCOUNT_LOCKED_MESSAGE);
      expect(locked.text).not.toContain("accessToken");
    } finally {
      expect((await setActive(true)).status).toBe(HTTP_OK);
    }

    const unlocked = await loginRequest(
      request,
      "customer",
      loginCustomer.email,
      loginCustomer.password,
    );
    expect(unlocked.status).toBe(HTTP_OK);
    expect(unlocked.json?.data?.accessToken).toBeTruthy();
  });
});

test.describe("Reset mật khẩu bằng token dùng một lần (A6)", () => {
  test("A6: link trong email đặt lại được mật khẩu đúng một lần, dùng lại link thì bị từ chối và mật khẩu không đổi", async ({
    page,
    request,
  }) => {
    test.setTimeout(UI_TEST_TIMEOUT_MS);
    if (!mock) throw new Error("Outbox email giả lập chưa được bật.");
    const { email, password: oldPassword } = resetCustomer;

    // 1) Quên mật khẩu trên giao diện; backend gửi thư vào outbox giả lập.
    const requestedAt = Date.now();
    await page.goto("/auth/forgot-password");
    await page.getByLabel("Email", { exact: true }).fill(email);
    await page.getByRole("button", { name: "Gửi liên kết đặt lại mật khẩu" }).click();
    await expect(page.getByRole("heading", { name: FORGOT_PASSWORD_SENT_HEADING })).toBeVisible({ timeout: 15_000 });

    // 2) Lấy token từ link trong thư gửi tới đúng khách này.
    const token = await waitForEmailToken(mock, "passwordReset", email, requestedAt);

    // 3) Mở link và đặt mật khẩu mới: thành công, chuyển về trang đăng nhập.
    await page.goto(`/auth/reset-password?token=${encodeURIComponent(token)}`);
    await page.getByLabel("Mật khẩu mới", { exact: true }).fill(NEW_PASSWORD);
    await page.getByLabel("Xác nhận mật khẩu mới", { exact: true }).fill(NEW_PASSWORD);
    await page.getByRole("button", { name: "Đặt lại mật khẩu" }).click();
    await expect(page).toHaveURL(/\/auth\/login$/);

    // 4) Mật khẩu cũ hết hiệu lực, mật khẩu mới đăng nhập được.
    const withOld = await loginRequest(request, "customer", email, oldPassword);
    expect(withOld.status).toBe(HTTP_UNAUTHORIZED);
    const withNew = await loginRequest(request, "customer", email, NEW_PASSWORD);
    expect(withNew.status).toBe(HTTP_OK);
    expect(withNew.json?.data?.accessToken).toBeTruthy();

    // 5) Dùng lại đúng link đó trên giao diện: bị từ chối, vẫn ở trang reset.
    await page.goto(`/auth/reset-password?token=${encodeURIComponent(token)}`);
    await page.getByLabel("Mật khẩu mới", { exact: true }).fill(OTHER_PASSWORD);
    await page.getByLabel("Xác nhận mật khẩu mới", { exact: true }).fill(OTHER_PASSWORD);
    await page.getByRole("button", { name: "Đặt lại mật khẩu" }).click();
    await expect(page.getByTestId("form-error-alert")).toHaveText(TOKEN_REJECTED_MESSAGE);
    await expect(page).toHaveURL(/\/auth\/reset-password\?token=/);

    // 6) Dùng lại qua API cũng bị từ chối 400, và mật khẩu KHÔNG bị đổi sang giá trị của lần dùng lại.
    const replay = await callApi(request, {
      method: "POST",
      path: "/auth/reset-password",
      data: { token, newPassword: OTHER_PASSWORD },
    });
    expect(replay.status).toBe(HTTP_BAD_REQUEST);
    expect(replay.json?.message).toBe(TOKEN_REJECTED_MESSAGE);

    const withReplayedPassword = await loginRequest(request, "customer", email, OTHER_PASSWORD);
    expect(withReplayedPassword.status).toBe(HTTP_UNAUTHORIZED);
    const stillNew = await loginRequest(request, "customer", email, NEW_PASSWORD);
    expect(stillNew.status).toBe(HTTP_OK);
  });
});

test.describe("Buộc đổi mật khẩu khi dùng mật khẩu tạm (A8)", () => {
  test("A8: nhân viên mật khẩu tạm bị chặn mọi API nghiệp vụ cho tới khi đổi mật khẩu thành công", async ({
    request,
  }) => {
    const { email, password: temporaryPassword } = employeeApi;

    const login = await loginRequest(request, "admin", email, temporaryPassword);
    expect(login.status).toBe(HTTP_OK);
    expect(login.json?.data?.user.mustChangePassword).toBe(true);
    const token = login.json?.data?.accessToken;
    expect(token).toBeTruthy();

    // Bị chặn: API nghiệp vụ trả 403 kèm mã PASSWORD_CHANGE_REQUIRED.
    const blocked = await callApi(request, { path: "/admin/bookings", token });
    expect(blocked.status).toBe(HTTP_FORBIDDEN);
    expect(blocked.json?.errorCode).toBe(PASSWORD_CHANGE_REQUIRED_CODE);

    // Vẫn được phép: xem hồ sơ phiên (/auth/me) để giao diện biết phải đổi mật khẩu.
    const me = await callApi<{ mustChangePassword: boolean }>(request, { path: "/auth/me", token });
    expect(me.status).toBe(HTTP_OK);
    expect(me.json?.data?.mustChangePassword).toBe(true);

    // Đổi mật khẩu bị từ chối khi sai mật khẩu hiện tại hoặc mật khẩu mới trùng mật khẩu tạm.
    const changePassword = (currentPassword: string, newPassword: string) =>
      callApi(request, {
        method: "POST",
        path: "/users/me/change-password",
        token,
        data: { currentPassword, newPassword },
      });
    expect((await changePassword(WRONG_PASSWORD, NEW_PASSWORD)).status).toBe(HTTP_BAD_REQUEST);
    expect((await changePassword(temporaryPassword, temporaryPassword)).status).toBe(
      HTTP_BAD_REQUEST,
    );
    const stillBlocked = await callApi(request, { path: "/admin/bookings", token });
    expect(stillBlocked.status).toBe(HTTP_FORBIDDEN);

    // Đổi thành công: mật khẩu tạm hết hiệu lực, đăng nhập lại bằng mật khẩu mới thì không còn bị chặn.
    expect((await changePassword(temporaryPassword, NEW_PASSWORD)).status).toBe(HTTP_OK);

    const withTemporary = await loginRequest(request, "admin", email, temporaryPassword);
    expect(withTemporary.status).toBe(HTTP_UNAUTHORIZED);

    const relogin = await loginRequest(request, "admin", email, NEW_PASSWORD);
    expect(relogin.status).toBe(HTTP_OK);
    expect(relogin.json?.data?.user.mustChangePassword).toBe(false);
    const freeToken = relogin.json?.data?.accessToken;
    const allowed = await callApi(request, { path: "/admin/bookings", token: freeToken });
    expect(allowed.status).toBe(HTTP_OK);
  });

  test("A8: nhân viên đăng nhập bằng mật khẩu tạm bị buộc sang trang đổi mật khẩu, đổi xong phải đăng nhập lại mới vào được trang làm việc", async ({
    page,
    request,
  }) => {
    test.setTimeout(UI_TEST_TIMEOUT_MS);
    const { email, password: temporaryPassword } = employeeUi;

    await loginViaAdminForm(page, email, temporaryPassword);
    await expect(page).toHaveURL(/\/admin\/change-password$/);
    await expect(page.getByRole("heading", { name: "Đổi mật khẩu", level: 1 })).toBeVisible();

    // Muốn vào trang làm việc cũng bị đưa ngược về trang đổi mật khẩu.
    await page.goto("/admin/bookings/counter");
    await expect(page).toHaveURL(/\/admin\/change-password$/);

    await page.getByLabel("Mật khẩu hiện tại", { exact: true }).fill(temporaryPassword);
    await page.getByLabel("Mật khẩu mới", { exact: true }).fill(NEW_PASSWORD);
    await page.getByLabel("Xác nhận mật khẩu mới", { exact: true }).fill(NEW_PASSWORD);
    await page.getByRole("button", { name: "Đổi mật khẩu", exact: true }).click();

    // Đổi xong bị đăng xuất; mật khẩu tạm không còn dùng được.
    await expect(page).toHaveURL(/\/admin\/login$/);
    const withTemporary = await loginRequest(request, "admin", email, temporaryPassword);
    expect(withTemporary.status).toBe(HTTP_UNAUTHORIZED);

    // Đăng nhập lại bằng mật khẩu mới: vào thẳng trang làm việc của nhân viên, không bị buộc đổi nữa.
    await loginViaAdminForm(page, email, NEW_PASSWORD);
    await expect(page).toHaveURL(/\/admin\/bookings\/counter$/);
  });
});
