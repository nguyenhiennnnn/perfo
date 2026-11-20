// Helper dùng chung cho các script seed chạy bằng Node (>= 18, dùng fetch có sẵn, không cần cài gói).
// k6 module trong loadtest/lib/ không dùng được ở đây vì chúng import từ "k6".

const trim = (value) => value.replace(/\/+$/, '');

export const BASE_URL = trim(process.env.BASE_URL || 'http://localhost:8080');
export const API_URL = `${BASE_URL}/api/v1`;

const MAX_ATTEMPTS = 20;
const DEFAULT_RETRY_SECONDS = 10;
const MAX_RETRY_SECONDS = 70;

// Lỗi không bắt được (ví dụ mất kết nối): in một dòng thay vì stack trace.
for (const event of ['unhandledRejection', 'uncaughtException']) {
  process.on(event, (error) => {
    console.error(error instanceof Error ? error.message : error);
    process.exit(1);
  });
}

export const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));

// Parse `--khoa=gia_tri` và `--co` thành object. Đối số không có dạng `--` bị bỏ qua.
export function parseArgs(argv) {
  const args = {};
  for (const raw of argv) {
    if (!raw.startsWith('--')) continue;
    const eq = raw.indexOf('=');
    if (eq === -1) args[raw.slice(2)] = true;
    else args[raw.slice(2, eq)] = raw.slice(eq + 1);
  }
  return args;
}

// Chặn chạy nhầm vào server thật: mọi host không phải localhost phải có cờ --yes.
export function assertSafeTarget(args) {
  const host = new URL(BASE_URL).hostname;
  const isLocal = host === 'localhost' || host === '127.0.0.1' || host === '::1';
  if (isLocal || args.yes) return;
  console.error(
    `BASE_URL = ${BASE_URL} không phải localhost.\n` +
      'Script này tạo dữ liệu thật. Chỉ chạy trên STAGING; nếu chắc chắn, thêm cờ --yes.',
  );
  process.exit(2);
}

let warned429 = false;

// Gọi API, trả { status, body, text }. Không ném lỗi theo HTTP status; chỉ tự thử lại khi gặp 429
// (rate limit theo IP), chờ theo Retry-After. Lỗi mạng được ném ra.
export async function request(method, path, { token, body, headers } = {}) {
  const url = path.startsWith('http') ? path : `${API_URL}${path}`;

  for (let attempt = 1; ; attempt++) {
    const res = await fetch(url, {
      method,
      headers: {
        'Content-Type': 'application/json',
        // Các endpoint [CsrfProtection] (login, admin-login) bắt buộc header này.
        'X-Requested-With': 'XMLHttpRequest',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
        ...headers,
      },
      body: body === undefined ? undefined : JSON.stringify(body),
    }).catch((error) => {
      const code = error?.cause?.code || error?.message || 'lỗi mạng';
      throw new Error(`Không kết nối được ${method} ${url} (${code}). Backend đã chạy và BASE_URL đúng chưa?`);
    });

    if (res.status === 429 && attempt < MAX_ATTEMPTS) {
      if (!warned429) {
        warned429 = true;
        console.warn(
          '[429] Server đang giới hạn tốc độ (RateLimit). Script sẽ chờ Retry-After rồi thử lại. ' +
            'Nếu quá chậm, nâng các biến RateLimit__* trong .env.loadtest.example rồi khởi động lại backend.',
        );
      }
      const retryAfter = Number(res.headers.get('retry-after'));
      const seconds = Math.min(retryAfter > 0 ? retryAfter : DEFAULT_RETRY_SECONDS, MAX_RETRY_SECONDS);
      await sleep(seconds * 1000);
      continue;
    }

    const text = await res.text();
    let json = null;
    try {
      json = text ? JSON.parse(text) : null;
    } catch {
      json = null;
    }
    return { status: res.status, body: json, text };
  }
}

// Đăng nhập admin (POST /auth/admin-login); trả access token hoặc thoát với thông báo rõ ràng.
export async function adminLogin() {
  const email = process.env.ADMIN_EMAIL || 'admin@cinema.local';
  const password = process.env.ADMIN_PASSWORD || 'Admin@123';
  const res = await request('POST', '/auth/admin-login', { body: { email, password } });
  const token = res.body?.data?.accessToken;
  if (res.status !== 200 || !token) {
    console.error(`Đăng nhập admin (${email}) thất bại: HTTP ${res.status} ${res.text}`);
    process.exit(1);
  }
  return token;
}
