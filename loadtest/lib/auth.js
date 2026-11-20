import http from 'k6/http';
import exec from 'k6/execution';
import { sleep } from 'k6';
import { SharedArray } from 'k6/data';
import {
  CSRF_HEADERS,
  DEFAULT_RETRY_AFTER_SECONDS,
  ENDPOINTS,
  JSON_HEADERS,
  LOGIN_MAX_ATTEMPTS,
} from './config.js';
import { checkOk, parseBody, trackStatus } from './checks.js';

// Danh sách tài khoản: [{ email, password, portal }]; portal = "customer" (mặc định) | "admin".
// Đọc một lần, dùng chung giữa các VU.
export const users = new SharedArray('users', function () {
  const list = JSON.parse(open('../data/users.json'));
  if (!Array.isArray(list) || list.length === 0) {
    throw new Error('loadtest/data/users.json phải là mảng có ít nhất một tài khoản.');
  }
  return list;
});

// Chọn tài khoản theo VU (vòng lại nếu ít tài khoản hơn VU). Mỗi VU luôn dùng cùng một tài khoản.
export function userForVu() {
  return users[(exec.vu.idInTest - 1) % users.length];
}

function loginUrl(user) {
  // Admin và Employee bắt buộc dùng /auth/admin-login; khách dùng /auth/login.
  return user.portal === 'admin' ? ENDPOINTS.adminLogin : ENDPOINTS.login;
}

// Đăng nhập bằng API. Trả { accessToken, expiresIn, user } hoặc ném lỗi nếu thất bại.
// Endpoint auth bị rate limit theo IP (mặc định 5 lần/60 giây): gặp 429 thì chờ Retry-After rồi thử lại.
// options (tùy chọn): { tags } gộp thêm tag vào request login; { onResponse(res) } được gọi với response
// cuối cùng (không gọi cho các lần 429 sẽ thử lại), trước khi kiểm tra, để kịch bản ghi metric riêng.
export function login(user, options) {
  const opts = options || {};
  for (let attempt = 1; ; attempt++) {
    const res = http.post(
      loginUrl(user),
      JSON.stringify({ email: user.email, password: user.password }),
      {
        headers: Object.assign({}, JSON_HEADERS, CSRF_HEADERS),
        tags: Object.assign({ name: 'POST /auth/login' }, opts.tags),
      },
    );

    if (res.status === 429 && attempt < LOGIN_MAX_ATTEMPTS) {
      trackStatus(res, 'POST /auth/login');
      const retryAfter = Number(res.headers['Retry-After']);
      sleep(retryAfter > 0 ? retryAfter : DEFAULT_RETRY_AFTER_SECONDS);
      continue;
    }

    if (opts.onResponse) opts.onResponse(res);

    if (!checkOk(res, 'login')) {
      throw new Error(`Đăng nhập thất bại (${user.email}): HTTP ${res.status} ${res.body}`);
    }

    // ApiResponseFilter bọc kết quả: { success, data: { accessToken, expiresIn, user } }.
    const data = parseBody(res).data;
    if (!data || !data.accessToken) {
      throw new Error(`Phản hồi đăng nhập (${user.email}) không có accessToken.`);
    }
    return { accessToken: data.accessToken, expiresIn: data.expiresIn, user: data.user };
  }
}

// Token được cache theo VU (biến module riêng cho từng VU) để không đăng nhập lại mỗi vòng lặp.
let cached = null;
const REFRESH_MARGIN_MS = 30 * 1000;

// Trả access token của tài khoản gán cho VU hiện tại; đăng nhập lại khi sắp hết hạn.
export function getToken() {
  const now = Date.now();
  if (cached === null || now >= cached.expiresAtMs - REFRESH_MARGIN_MS) {
    const session = login(userForVu());
    cached = { token: session.accessToken, expiresAtMs: now + session.expiresIn * 1000 };
  }
  return cached.token;
}

// Header cho request cần xác thực.
export function authHeaders(token) {
  return Object.assign({}, JSON_HEADERS, { Authorization: `Bearer ${token}` });
}
