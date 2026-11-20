// Cấu hình dùng chung cho mọi kịch bản k6. Đọc từ biến môi trường: k6 run -e BASE_URL=... script.js

function trimTrailingSlashes(value) {
  return value.replace(/\/+$/, '');
}

// Gốc server (không kèm /api/v1). Mặc định trỏ backend chạy local (launchSettings: cổng 8080).
export const BASE_URL = trimTrailingSlashes(__ENV.BASE_URL || 'http://localhost:8080');

// Tiền tố API thật của backend (ApiPaths.ApiVersionPrefix = "api/v1").
export const API_URL = `${BASE_URL}/api/v1`;

// Backend yêu cầu header này ở các endpoint [CsrfProtection] (login, admin-login, refresh, logout).
// Không gửi Origin: filter chỉ kiểm Origin khi header đó có mặt.
export const CSRF_HEADERS = { 'X-Requested-With': 'XMLHttpRequest' };

export const JSON_HEADERS = { 'Content-Type': 'application/json' };

// Số lần thử tối đa khi login bị 429, và thời gian chờ mặc định nếu thiếu Retry-After (giây).
export const LOGIN_MAX_ATTEMPTS = Number(__ENV.LOGIN_MAX_ATTEMPTS || 3);
export const DEFAULT_RETRY_AFTER_SECONDS = Number(__ENV.DEFAULT_RETRY_AFTER_SECONDS || 10);

// Endpoint thật (xem backend/Cinema.Api/Controllers).
export const ENDPOINTS = {
  login: `${API_URL}/auth/login`, // khách hàng
  adminLogin: `${API_URL}/auth/admin-login`, // admin + nhân viên
  me: `${API_URL}/auth/me`,
  health: `${API_URL}/health`,
};
