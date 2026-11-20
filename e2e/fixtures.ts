import {
  test as base,
  expect,
  type Browser,
  type Page,
  type PlaywrightWorkerArgs,
} from "@playwright/test";
import { loginViaApi, type LoginSession, type Role } from "./helpers/auth";

export { expect };

/** Khóa localStorage mà frontend dùng làm "session hint" (BRAND.storageKeys.sessionHint). */
const SESSION_HINT_KEY = "perfo_session";

/** Đăng nhập lại khi access token còn dưới ngưỡng này (giây). */
const MIN_REMAINING_SECONDS = 300;

interface CachedSession {
  session: LoginSession;
  expiresAt: number;
}

/**
 * Cache theo worker: mỗi role chỉ đăng nhập API một lần cho nhiều test, vì
 * /auth/login, /auth/admin-login và /auth/refresh dùng chung rate limit (5 lần / 60 giây).
 */
const sessionCache = new Map<Role, CachedSession>();

async function getSession(
  playwright: PlaywrightWorkerArgs["playwright"],
  role: Role,
): Promise<LoginSession> {
  const cached = sessionCache.get(role);
  if (cached && cached.expiresAt - Date.now() > MIN_REMAINING_SECONDS * 1000) {
    return cached.session;
  }

  const api = await playwright.request.newContext();
  try {
    const session = await loginViaApi(api, role);
    sessionCache.set(role, {
      session,
      expiresAt: Date.now() + session.expiresIn * 1000,
    });
    return session;
  } finally {
    await api.dispose();
  }
}

/**
 * Tạo Page (context riêng) đã đăng nhập theo role:
 * - Đăng nhập bằng API để lấy access token + user.
 * - Đặt session hint vào localStorage để SessionInitializer của app chủ động khôi phục phiên.
 * - Trả lời POST /auth/refresh bằng chính phiên vừa đăng nhập, nên app không phụ thuộc
 *   cookie refresh và không tốn rate limit mỗi lần tải trang.
 */
async function openAuthenticatedPage(
  browser: Browser,
  playwright: PlaywrightWorkerArgs["playwright"],
  role: Role,
): Promise<{ page: Page; close: () => Promise<void> }> {
  const session = await getSession(playwright, role);
  const context = await browser.newContext();

  await context.addInitScript((key) => {
    try {
      window.localStorage.setItem(key, "1");
    } catch {
      // Bỏ qua: trang không cho truy cập localStorage (ví dụ about:blank).
    }
  }, SESSION_HINT_KEY);

  await context.route("**/auth/refresh", async (route) => {
    const request = route.request();
    if (request.method() === "OPTIONS") return route.fallback();

    // Phản hồi giả lập đi qua kiểm tra CORS của trình duyệt (frontend và API khác origin).
    const origin = request.headers()["origin"];
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      headers: origin
        ? {
            "Access-Control-Allow-Origin": origin,
            "Access-Control-Allow-Credentials": "true",
          }
        : {},
      body: JSON.stringify(session.body),
    });
  });

  const page = await context.newPage();
  return { page, close: () => context.close() };
}

interface AuthFixtures {
  adminPage: Page;
  customerPage: Page;
  employeePage: Page;
}

function authenticatedPageFixture(role: Role) {
  return async (
    {
      browser,
      playwright,
    }: { browser: Browser; playwright: PlaywrightWorkerArgs["playwright"] },
    use: (page: Page) => Promise<void>,
  ) => {
    const { page, close } = await openAuthenticatedPage(
      browser,
      playwright,
      role,
    );
    try {
      await use(page);
    } finally {
      await close();
    }
  };
}

export const test = base.extend<AuthFixtures>({
  adminPage: authenticatedPageFixture("admin"),
  customerPage: authenticatedPageFixture("customer"),
  employeePage: authenticatedPageFixture("employee"),
});
