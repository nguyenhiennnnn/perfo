import type { APIRequestContext } from "@playwright/test";
import { API_BASE_URL } from "./auth";

/**
 * Gọi API thô cho các test phân quyền và xác thực: trả về status + body mà KHÔNG ném lỗi theo HTTP status,
 * để test tự khẳng định 401/403/400. Khác với helper đặt vé (ném lỗi khi không thành công).
 * Gặp 429 (rate limit) thì chờ Retry-After rồi thử lại, giống loginViaApi ở ./auth.
 */

const CSRF_HEADERS = { "X-Requested-With": "XMLHttpRequest" };
const HTTP_TOO_MANY_REQUESTS = 429;
const MAX_ATTEMPTS = 3;
const DEFAULT_RETRY_SECONDS = 60;

/** Body JSON chuẩn của API (ApiResponse): thành công có data, lỗi có message/errors/errorCode. */
export interface ApiBody<T = unknown> {
  success?: boolean;
  data?: T | null;
  message?: string;
  errors?: Record<string, string[]>;
  errorCode?: string;
}

export interface ApiCallResult<T = unknown> {
  status: number;
  contentType: string;
  /** Body dạng chuỗi (UTF-8). */
  text: string;
  /** Body dạng byte, dùng cho file (PDF). */
  buffer: Buffer;
  /** Body đã parse JSON; undefined nếu không phải JSON. */
  json?: ApiBody<T>;
}

export interface ApiCallOptions {
  method?: "GET" | "POST" | "PUT" | "DELETE";
  /** Đường dẫn tính từ API_BASE_URL, ví dụ "/bookings/12". */
  path: string;
  /** Access token; bỏ trống để gọi ẩn danh. */
  token?: string;
  data?: Record<string, unknown>;
}

export async function callApi<T = unknown>(
  request: APIRequestContext,
  { method = "GET", path, token, data }: ApiCallOptions,
): Promise<ApiCallResult<T>> {
  for (let attempt = 1; ; attempt++) {
    const response = await request.fetch(`${API_BASE_URL}${path}`, {
      method,
      headers: {
        ...CSRF_HEADERS,
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
      data,
      failOnStatusCode: false,
    });

    if (response.status() === HTTP_TOO_MANY_REQUESTS && attempt < MAX_ATTEMPTS) {
      const retryAfter = Number(response.headers()["retry-after"]);
      const seconds = retryAfter > 0 ? retryAfter : DEFAULT_RETRY_SECONDS;
      await new Promise((resolve) => setTimeout(resolve, seconds * 1000));
      continue;
    }

    const buffer = await response.body();
    const text = buffer.toString("utf8");
    let json: ApiBody<T> | undefined;
    try {
      json = JSON.parse(text) as ApiBody<T>;
    } catch {
      // Body không phải JSON (ví dụ file PDF hoặc phản hồi rỗng của 401/403 do policy).
    }

    return {
      status: response.status(),
      contentType: response.headers()["content-type"] ?? "",
      text,
      buffer,
      json,
    };
  }
}
