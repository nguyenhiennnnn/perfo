// L4 - Duyệt phim (chỉ đọc): tăng dần đến 200 user đồng thời, giữ 5 phút.
// Mỗi iteration là một lượt duyệt: GET /movies -> GET /showtimes?date= -> GET /showtimes/{id}/seats.
// Cả ba endpoint đều công khai (không cần đăng nhập) nên kịch bản không login, không ghi dữ liệu.
//
// setup(): gọi GET /showtimes?date=<ngày> MỘT lần, gom mọi showtimeId trong lịch; mỗi iteration chọn ngẫu nhiên
//          một id trong đó để xem sơ đồ ghế.
//
// Ngưỡng (xem options.thresholds):
//   - p95 < 400ms: tính chung mọi request duyệt (tag phase:browse) và riêng từng endpoint (tag step:<bước>)
//   - lỗi < 0,5%: Rate browse_errors (sai status, body success != true, lỗi mạng; 429 cũng tính là lỗi)
//
// Chạy (chỉ staging/local):
//   k6 run -e BASE_URL=http://localhost:8080 loadtest/scenarios/l4-browse.js
//   hoặc: npm run loadtest:l4 -- -e BASE_URL=http://localhost:8080
//   Chạy thử nhanh: thêm -e TARGET_VUS=10 -e RAMP_UP=10s -e HOLD=20s -e RAMP_DOWN=5s
//
// Biến môi trường riêng của kịch bản:
//   TARGET_VUS          số VU ở đỉnh (mặc định 200)
//   RAMP_UP             thời gian tăng dần từ 0 đến TARGET_VUS (mặc định 2m)
//   HOLD                thời gian giữ ở TARGET_VUS (mặc định 5m)
//   RAMP_DOWN           thời gian giảm về 0 (mặc định 30s)
//   THINK_TIME_SECONDS  thời gian nghỉ giữa hai lượt duyệt của một VU (mặc định 1)
//   DATE                ngày lấy lịch chiếu, yyyy-MM-dd (mặc định: hôm nay theo giờ Việt Nam)
//
// Điều kiện: ngày DATE có ít nhất một suất chiếu (trạng thái Scheduled) và đã nâng RateLimit Global
// (xem .env.loadtest.example). Limiter theo IP, mặc định 100 request/phút nên 200 VU từ một máy sẽ nhận 429.

import http from 'k6/http';
import { sleep } from 'k6';
import { Rate } from 'k6/metrics';
import { API_URL } from '../lib/config.js';
import { checkOk, parseBody } from '../lib/checks.js';

// ───────────────────────── Cấu hình ─────────────────────────

const TARGET_VUS = Number(__ENV.TARGET_VUS || 200);
const RAMP_UP = __ENV.RAMP_UP || '2m';
const HOLD = __ENV.HOLD || '5m';
const RAMP_DOWN = __ENV.RAMP_DOWN || '30s';
const THINK_TIME_SECONDS = Number(__ENV.THINK_TIME_SECONDS || 1);

const P95_LIMIT_MS = 400;
const MAX_ERROR_RATE = 0.005;

const STEPS = ['movies', 'showtimes', 'seats'];

// Tên request (gom URL động thành một nhóm metric).
const NAMES = {
  movies: 'GET /movies',
  showtimes: 'GET /showtimes',
  seats: 'GET /showtimes/{id}/seats',
};

// Backend lọc lịch chiếu theo ngày giờ Việt Nam (BusinessTime, Asia/Ho_Chi_Minh, UTC+7, không có DST).
const BUSINESS_UTC_OFFSET_MS = 7 * 60 * 60 * 1000;

// ───────────────────────── Metric ─────────────────────────

// Mỗi request duyệt ghi 1 mẫu: true nếu lỗi, false nếu đúng status và body success=true.
// (409/429 đã được lib/checks.js đếm riêng vào conflict_409 / rate_limited_429.)
const browseErrors = new Rate('browse_errors');

// ───────────────────────── Options ─────────────────────────

// Sub-metric theo tag chỉ được k6 theo dõi khi có threshold. Request setup gắn step:setup, phase:setup
// nên không lẫn vào các ngưỡng này.
const thresholds = {
  browse_errors: [`rate<${MAX_ERROR_RATE}`],
  'http_req_duration{phase:browse}': [`p(95)<${P95_LIMIT_MS}`],
};
for (const step of STEPS) {
  thresholds[`http_req_duration{step:${step}}`] = [`p(95)<${P95_LIMIT_MS}`];
}

export const options = {
  scenarios: {
    browse: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: RAMP_UP, target: TARGET_VUS },
        { duration: HOLD, target: TARGET_VUS },
        { duration: RAMP_DOWN, target: 0 },
      ],
      gracefulRampDown: '30s',
    },
  },
  thresholds,
};

// ───────────────────────── Hàm phụ ─────────────────────────

function fail(message) {
  throw new Error(message);
}

// Hôm nay theo giờ Việt Nam, dạng yyyy-MM-dd.
function todayInBusinessTime() {
  return new Date(Date.now() + BUSINESS_UTC_OFFSET_MS).toISOString().slice(0, 10);
}

// Một request duyệt: gắn tag, kiểm tra bằng checkOk (status 200 + success=true), ghi nhận browse_errors.
function browse(step, url) {
  const res = http.get(url, { tags: { name: NAMES[step], step, phase: 'browse' } });
  const ok = checkOk(res, NAMES[step]);
  browseErrors.add(!ok);
  return res;
}

// ───────────────────────── setup ─────────────────────────

export function setup() {
  const date = __ENV.DATE || todayInBusinessTime();
  if (!/^\d{4}-\d{2}-\d{2}$/.test(date)) fail(`DATE không hợp lệ: "${date}" (dạng yyyy-MM-dd).`);

  const res = http.get(`${API_URL}/showtimes?date=${date}`, {
    tags: { name: NAMES.showtimes, step: 'setup', phase: 'setup' },
  });
  if (res.status !== 200) {
    fail(`Không lấy được lịch chiếu ngày ${date}: HTTP ${res.status} ${res.body}`);
  }

  const movies = ((parseBody(res) || {}).data || {}).movies || [];
  const showtimeIds = [];
  for (const movie of movies) {
    for (const showtime of movie.showtimes || []) {
      if (Number.isInteger(showtime.showtimeId) && showtimeIds.indexOf(showtime.showtimeId) === -1) {
        showtimeIds.push(showtime.showtimeId);
      }
    }
  }
  if (showtimeIds.length === 0) {
    fail(
      `Không có suất chiếu nào ngày ${date}. Tạo suất chiếu (node loadtest/scripts/seed-showtime.mjs --yes) ` +
        'hoặc chọn ngày khác bằng -e DATE=yyyy-MM-dd.',
    );
  }

  console.log(`Lịch chiếu ngày ${date}: ${movies.length} phim, ${showtimeIds.length} suất chiếu.`);
  return { date, showtimeIds };
}

// ───────────────────────── Luồng một user ─────────────────────────

export default function (data) {
  const showtimeId = data.showtimeIds[Math.floor(Math.random() * data.showtimeIds.length)];

  browse('movies', `${API_URL}/movies`);
  browse('showtimes', `${API_URL}/showtimes?date=${data.date}`);
  browse('seats', `${API_URL}/showtimes/${showtimeId}/seats`);

  sleep(THINK_TIME_SECONDS);
}
