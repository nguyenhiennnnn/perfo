import type { APIRequestContext } from "@playwright/test";
import { API_BASE_URL } from "./auth";

/**
 * Đổi cài đặt rạp (PUT /admin/settings) trong lúc test và trả về giá trị cũ để khôi phục.
 * Đây là cài đặt dùng chung: chỉ chạy trên DB E2E riêng và luôn khôi phục trong finally.
 */

/** Các khóa chỉ có ở phản hồi GET /admin/settings, không thuộc UpdateSettingsRequest. */
const SETTINGS_RESPONSE_ONLY_KEYS = [
  "timeZoneId",
  "locale",
  "currencyCode",
  "currencySymbol",
  "defaultEmployeePassword",
] as const;

interface ApiEnvelope<T> {
  success: boolean;
  data: T;
}

export type SettingsPatch = Record<string, unknown>;

export async function updateCinemaSettings(
  request: APIRequestContext,
  adminAccessToken: string,
  patch: SettingsPatch,
): Promise<SettingsPatch> {
  const headers = { Authorization: `Bearer ${adminAccessToken}` };

  const getResponse = await request.get(`${API_BASE_URL}/admin/settings`, {
    headers,
    failOnStatusCode: false,
  });
  if (!getResponse.ok()) {
    throw new Error(
      `Đọc cài đặt rạp thất bại: HTTP ${getResponse.status()} ${await getResponse.text()}`,
    );
  }
  const current = ((await getResponse.json()) as ApiEnvelope<Record<string, unknown>>).data;

  const previous: SettingsPatch = {};
  for (const key of Object.keys(patch)) previous[key] = current[key];

  const body: Record<string, unknown> = { ...current, ...patch };
  for (const key of SETTINGS_RESPONSE_ONLY_KEYS) delete body[key];

  const putResponse = await request.put(`${API_BASE_URL}/admin/settings`, {
    headers,
    data: body,
    failOnStatusCode: false,
  });
  if (!putResponse.ok()) {
    throw new Error(
      `Cập nhật cài đặt rạp (${Object.keys(patch).join(", ")}) thất bại: HTTP ${putResponse.status()} ${await putResponse.text()}`,
    );
  }
  return previous;
}
