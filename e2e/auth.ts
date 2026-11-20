import type { APIRequestContext } from "@playwright/test";

export type Role = "admin" | "customer" | "employee";

/** Base URL của API backend (khớp VITE_API_BASE_URL trong .env.example). */
export const API_BASE_URL = (
  process.env.E2E_API_URL || "http://localhost:8080/api/v1"
).replace(/\/+$/, "");

interface Account {
  email: string;
  password: string;
  /** Customer dùng /auth/login; Admin và Employee bắt buộc dùng /auth/admin-login. */
  endpoint: "/auth/login" | "/auth/admin-login";
}

/** Tài khoản seed (backend/Cinema.Infrastructure/Persistence/Seed/DbInitializer.cs). */
export const ACCOUNTS: Record<Role, Account> = {
  admin: {
    email: "admin@cinema.local",
    password: "Admin@123",
    endpoint: "/auth/admin-login",
  },
  customer: {
    email: "customer@cinema.local",
    password: "Customer@123",
    endpoint: "/auth/login",
  },
  employee: {
    email: "employee@cinema.local",
    password: "Employee@123",
    endpoint: "/auth/admin-login",
  },
};

export interface LoginUser {
  userId: number;
  fullName: string;
  email: string;
  role: string;
  mustChangePassword?: boolean;
}

/** Body JSON API trả về: ApiResponseFilter bọc LoginResponse trong { success, data }. */
export interface LoginBody {
  success: boolean;
  data: { accessToken: string; expiresIn: number; user: LoginUser };
}

export interface LoginSession {
  accessToken: string;
  /** Thời hạn access token, tính bằng giây. */
  expiresIn: number;
  user: LoginUser;
  /** Nguyên body phản hồi, dùng lại nguyên vẹn cho /auth/refresh giả lập ở fixture. */
  body: LoginBody;
}

const CSRF_HEADERS = { "X-Requested-With": "XMLHttpRequest" };
const MAX_ATTEMPTS = 3;
const DEFAULT_RETRY_SECONDS = 60;

/**
 * Đăng nhập bằng API và trả về phiên (có accessToken).
 * Endpoint auth bị rate limit (mặc định 5 lần / 60 giây theo IP): nếu gặp 429
 * thì chờ theo Retry-After rồi thử lại tối đa MAX_ATTEMPTS lần.
 */
export async function loginViaApi(
  request: APIRequestContext,
  role: Role,
): Promise<LoginSession> {
  const { email, password, endpoint } = ACCOUNTS[role];

  for (let attempt = 1; ; attempt++) {
    const response = await request.post(`${API_BASE_URL}${endpoint}`, {
      data: { email, password },
      headers: CSRF_HEADERS,
      failOnStatusCode: false,
    });

    if (response.status() === 429 && attempt < MAX_ATTEMPTS) {
      const retryAfter = Number(response.headers()["retry-after"]);
      const seconds = retryAfter > 0 ? retryAfter : DEFAULT_RETRY_SECONDS;
      await new Promise((resolve) => setTimeout(resolve, seconds * 1000));
      continue;
    }

    if (!response.ok()) {
      throw new Error(
        `Đăng nhập API thất bại (${role}, ${email}): ${response.status()} ${await response.text()}`,
      );
    }

    const body = (await response.json()) as LoginBody;
    if (!body.data?.accessToken) {
      throw new Error(`Phản hồi đăng nhập (${role}) không có accessToken.`);
    }

    const { accessToken, expiresIn, user } = body.data;
    return { accessToken, expiresIn, user, body };
  }
}

/** Đăng nhập bằng API và chỉ trả về access token. */
export async function getAccessToken(
  request: APIRequestContext,
  role: Role,
): Promise<string> {
  return (await loginViaApi(request, role)).accessToken;
}
