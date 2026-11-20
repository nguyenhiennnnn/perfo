// L2 - Cao điểm đặt vé: tăng dần đến 100 user mới/phút, giữ 10 phút.
// Mỗi iteration là MỘT user mới đi hết luồng: login -> xem suất -> xem ghế -> giữ 1 ghế -> checkout.
//
// Ngưỡng (xem options.thresholds):
//   - p95 từng bước < 1s (Trend riêng cho mỗi bước: step_<bước>_duration)
//   - thành công >= 95% (Rate step_success; 409 tranh ghế KHÔNG tính vào mẫu số)
//   - 5xx < 0,5% (Rate server_error_5xx, tính trên mọi request)
//
// Chạy (chỉ staging/local):
//   k6 run -e BASE_URL=http://localhost:8080 -e SHOWTIME_IDS=101,102,...,111 loadtest/scenarios/l2-peak-booking.js
//   Chạy thử nhanh: thêm -e TARGET_RATE=5 -e RAMP_UP=30s -e HOLD=1m (ít ghế/suất chiếu cần hơn).
//
// Biến môi trường riêng của kịch bản:
//   SHOWTIME_IDS    danh sách showtimeId cách nhau dấu phẩy (hoặc SHOWTIME_ID cho một suất). Bắt buộc.
//   TARGET_RATE     user mới mỗi phút ở đỉnh (mặc định 100)
//   RAMP_UP         thời gian tăng dần từ 0 đến TARGET_RATE (mặc định 5m)
//   HOLD            thời gian giữ ở TARGET_RATE (mặc định 10m)
//   PAYMENT_METHOD  phương thức checkout (mặc định VNPay: backend chỉ tự ký URL, không gọi cổng ngoài;
//                   Momo/Stripe sẽ gọi sandbox ngoài, đừng dùng khi test tải)
//
// Cách chọn ghế (tránh xung đột giữa các iteration):
//   * Mỗi iteration có số thứ tự duy nhất i (exec.scenario.iterationInTest). Suất = i % số suất;
//     thứ tự ghế trong suất là một hoán vị cố định (bước nhảy nguyên tố cùng nhau với số ghế ứng viên)
//     nên nhìn như ngẫu nhiên nhưng hai iteration không bao giờ trùng ghế khi còn đủ chỗ.
//   * Chỉ dùng ghế thường (không phải ghế đôi) có số ghế = 1, 4, 7, ... trong hàng, để mỗi lần giữ ghế
//     không tạo "ghế trống lẻ" cạnh ghế vừa chọn (SeatLayoutRules.ValidateSelection, chế độ Strict
//     trả 422). Hệ quả: một phòng 20 x 16 chỉ cho 120 ghế ứng viên/suất, nên cần nhiều suất chiếu.
//     setup() tự tính và dừng sớm nếu số ghế không đủ cho số iteration dự kiến.
//   * Tài khoản: iteration i dùng tài khoản khách thứ i % N (N giảm bớt sao cho nguyên tố cùng nhau với số
//     suất), nên một tài khoản không bao giờ giữ ghế hai lần ở cùng một suất trong khi booking cũ còn hiệu lực
//     (backend trả 409 "đã có booking chờ").
//
// Điều kiện: đã nâng RateLimit (xem .env.loadtest.example: Global, Auth và BookingHold), có đủ tài khoản
// khách trong loadtest/data/users.json và suất chiếu mới, chưa có ghế bị giữ.

import http from 'k6/http';
import exec from 'k6/execution';
import { check } from 'k6';
import { Rate, Trend } from 'k6/metrics';
import { API_URL } from '../lib/config.js';
import { users, login, authHeaders } from '../lib/auth.js';
import { checkOk, checkOkOrConflict, parseBody } from '../lib/checks.js';

// ───────────────────────── Cấu hình ─────────────────────────

const TARGET_RATE = Number(__ENV.TARGET_RATE || 100); // iteration (user mới) mỗi phút
const RAMP_UP = __ENV.RAMP_UP || '5m';
const HOLD = __ENV.HOLD || '10m';
const PAYMENT_METHOD = __ENV.PAYMENT_METHOD || 'VNPay';

const SHOWTIME_IDS = String(__ENV.SHOWTIME_IDS || __ENV.SHOWTIME_ID || '')
  .split(',')
  .map((s) => s.trim())
  .filter((s) => s !== '')
  .map(Number);

const STEPS = ['login', 'showtimes', 'seats', 'hold', 'checkout'];
const P95_LIMIT_MS = 1000;
const MIN_SUCCESS_RATE = 0.95;
const MAX_5XX_RATE = 0.005;

// Tên request (gom URL động) và tag step của từng bước.
const NAMES = {
  login: 'POST /auth/login',
  showtimes: 'GET /showtimes',
  seats: 'GET /showtimes/{id}/seats',
  hold: 'POST /bookings/hold',
  checkout: 'POST /bookings/{id}/checkout',
};

// Backend lọc lịch chiếu theo ngày giờ Việt Nam (BusinessTime, Asia/Ho_Chi_Minh, UTC+7, không có DST).
const BUSINESS_UTC_OFFSET_MS = 7 * 60 * 60 * 1000;

// Mẫu số ghế: số ghế ứng viên là số ghế có (seatNumber - 1) chia hết cho SEAT_SPACING.
const SEAT_SPACING = 3;

// ───────────────────────── Metric ─────────────────────────

// Một Trend riêng cho mỗi bước (đơn vị ms): step_login_duration, step_showtimes_duration, ...
const stepDuration = {};
for (const step of STEPS) stepDuration[step] = new Trend(`step_${step}_duration`, true);

// Mỗi lần thử một bước ghi 1 mẫu true/false, gắn tag step. Bước hold gặp 409 tranh ghế không ghi mẫu nào.
const stepSuccess = new Rate('step_success');

// Mọi request có status >= 500 ghi true, ngược lại false.
const server5xx = new Rate('server_error_5xx');

// ───────────────────────── Options ─────────────────────────

const thresholds = {
  step_success: [`rate>=${MIN_SUCCESS_RATE}`],
  server_error_5xx: [`rate<${MAX_5XX_RATE}`],
};
for (const step of STEPS) {
  thresholds[`step_${step}_duration`] = [`p(95)<${P95_LIMIT_MS}`];
  // Sub-metric theo tag chỉ được k6 theo dõi khi có threshold: để thấy tỉ lệ thành công từng bước.
  thresholds[`step_success{step:${step}}`] = [`rate>=${MIN_SUCCESS_RATE}`];
}

export const options = {
  setupTimeout: '2m',
  scenarios: {
    peak_booking: {
      executor: 'ramping-arrival-rate',
      startRate: 0,
      timeUnit: '1m',
      preAllocatedVUs: 50,
      maxVUs: 300, // login có thể chờ 429 vài chục giây; dư VU để không rơi iteration (dropped_iterations)
      gracefulStop: '60s',
      stages: [
        { duration: RAMP_UP, target: TARGET_RATE },
        { duration: HOLD, target: TARGET_RATE },
      ],
    },
  },
  thresholds,
};

// ───────────────────────── Hàm phụ ─────────────────────────

function fail(message) {
  throw new Error(message);
}

function gcd(a, b) {
  while (b !== 0) {
    const t = a % b;
    a = b;
    b = t;
  }
  return a;
}

function toSeconds(text) {
  const m = /^(\d+(?:\.\d+)?)(s|m|h)$/.exec(String(text).trim());
  if (!m) fail(`Khoảng thời gian không hợp lệ: "${text}" (ví dụ 30s, 5m, 1h).`);
  return Number(m[1]) * { s: 1, m: 60, h: 3600 }[m[2]];
}

// Số iteration dự kiến: ramp 0 -> TARGET_RATE (trung bình TARGET_RATE/2) rồi giữ nguyên.
function expectedIterations() {
  return Math.ceil(
    ((toSeconds(RAMP_UP) / 60) * TARGET_RATE) / 2 + (toSeconds(HOLD) / 60) * TARGET_RATE,
  );
}

// Tài khoản khách (bỏ admin), lấy theo chỉ số để khỏi sao chép dữ liệu dùng chung.
const customerIdx = [];
for (let i = 0; i < users.length; i++) {
  if (users[i].portal !== 'admin') customerIdx.push(i);
}
// Số tài khoản dùng thực tế: nguyên tố cùng nhau với số suất, để cặp (tài khoản, suất) = (i % N, i % M)
// không lặp lại trong N * M iteration đầu.
let accountCount = customerIdx.length;
while (accountCount > 1 && gcd(accountCount, Math.max(SHOWTIME_IDS.length, 1)) !== 1) accountCount--;

// Bước nhảy cho hoán vị ghế: gần tỉ lệ vàng của n và nguyên tố cùng nhau với n.
function strideFor(n) {
  if (n <= 1) return 1;
  let stride = Math.max(1, Math.round(n * 0.618));
  while (gcd(stride, n) !== 1) stride++;
  return stride;
}

// Ghế ứng viên theo CẤU TRÚC sơ đồ (không phụ thuộc trạng thái), thứ tự ổn định: hàng A, B, ..., số ghế tăng.
// Ghế p được chọn khi (p - 1) % SEAT_SPACING == 0, không phải ghế đôi, và với mỗi bên: hoặc không có ghế
// liền kề, hoặc còn ghế cách một (p ± 2), để ghế liền kề không bao giờ thành ghế trống lẻ.
function candidateSeats(seats) {
  const rows = {};
  for (const s of seats) (rows[s.seatRow] = rows[s.seatRow] || []).push(s);

  const names = Object.keys(rows).sort((a, b) => a.length - b.length || (a < b ? -1 : a > b ? 1 : 0));
  const out = [];
  for (const name of names) {
    const row = rows[name].sort((a, b) => a.seatNumber - b.seatNumber);
    const present = {};
    for (const s of row) present[s.seatNumber] = true;
    const leavesLoneGap = (p, d) => present[p + d] === true && present[p + 2 * d] !== true;

    for (const s of row) {
      const p = s.seatNumber;
      if ((p - 1) % SEAT_SPACING !== 0) continue;
      if (s.isCouple || s.coupleId) continue;
      if (leavesLoneGap(p, -1) || leavesLoneGap(p, 1)) continue;
      out.push(s);
    }
  }
  return out;
}

// Ngày (giờ Việt Nam, yyyy-MM-dd) của thời điểm startTime mà API trả về (chuỗi thiếu múi giờ coi là UTC).
function businessDate(startTime) {
  const text = String(startTime);
  const ms = Date.parse(/(Z|[+-]\d\d:\d\d)$/.test(text) ? text : `${text}Z`);
  if (Number.isNaN(ms)) fail(`startTime không hợp lệ: ${text}`);
  return new Date(ms + BUSINESS_UTC_OFFSET_MS).toISOString().slice(0, 10);
}

function getSeatMap(showtimeId, tags) {
  const res = http.get(`${API_URL}/showtimes/${showtimeId}/seats`, { tags });
  return { res, data: res.status === 200 ? (parseBody(res) || {}).data : null };
}

// ───────────────────────── setup ─────────────────────────

export function setup() {
  if (SHOWTIME_IDS.length === 0) {
    fail('Thiếu SHOWTIME_IDS. Chạy: k6 run -e SHOWTIME_IDS=101,102,... (lấy từ seed-showtime.mjs).');
  }
  for (const id of SHOWTIME_IDS) {
    if (!Number.isInteger(id) || id <= 0) fail(`showtimeId không hợp lệ: ${id}`);
  }
  if (new Set(SHOWTIME_IDS).size !== SHOWTIME_IDS.length) fail('SHOWTIME_IDS có id bị trùng.');
  if (!(TARGET_RATE > 0)) fail('TARGET_RATE phải > 0.');
  if (customerIdx.length === 0) fail('Không có tài khoản khách nào trong loadtest/data/users.json.');

  const total = expectedIterations();

  // Mỗi cặp (tài khoản, suất) chỉ xuất hiện một lần trong accountCount * số suất iteration đầu.
  if (accountCount * SHOWTIME_IDS.length < total) {
    fail(
      `Cần accountCount x số suất >= ${total} iteration dự kiến, hiện ${accountCount} x ${SHOWTIME_IDS.length}. ` +
        'Thêm tài khoản (node loadtest/scripts/seed-users.mjs --yes) hoặc thêm suất chiếu.',
    );
  }

  const showtimes = [];
  let capacity = 0;
  for (const id of SHOWTIME_IDS) {
    const { res, data } = getSeatMap(id, { name: NAMES.seats, step: 'setup' });
    if (!data || !Array.isArray(data.seats) || data.seats.length === 0) {
      fail(`Không lấy được sơ đồ ghế của suất ${id}: HTTP ${res.status} ${res.body}`);
    }
    const candidates = candidateSeats(data.seats);
    const free = candidates.filter((s) => s.status === 'Available').length;
    capacity += free;
    showtimes.push({ id, date: businessDate(data.startTime), candidates: candidates.length, free });
    console.log(`Suất ${id}: ngày ${showtimes[showtimes.length - 1].date}, ${free}/${candidates.length} ghế ứng viên còn trống`);
  }

  if (capacity < total) {
    const perShowtime = Math.max(1, Math.round(capacity / SHOWTIME_IDS.length));
    fail(
      `Không đủ ghế: ${capacity} ghế ứng viên còn trống < ${total} iteration dự kiến. ` +
        `Cần khoảng ${Math.ceil(total / perShowtime)} suất (hiện ${SHOWTIME_IDS.length}). ` +
        'Tạo thêm suất chiếu mới (seed-showtime.mjs --new --start=...) hoặc giảm TARGET_RATE/HOLD.',
    );
  }

  return { showtimes };
}

// ───────────────────────── Luồng một user ─────────────────────────

let loggedFailures = 0; // giới hạn log lỗi theo VU để không tràn màn hình

function logFailure(message) {
  if (loggedFailures++ < 3) console.error(message);
}

// Ghi nhận Trend, 5xx và (nếu ok !== null) tỉ lệ thành công của một request.
function record(step, res, ok) {
  stepDuration[step].add(res.timings.duration);
  server5xx.add(res.status >= 500);
  if (ok !== null) stepSuccess.add(ok, { step });
}

function hasConflictSeatIds(res) {
  const body = parseBody(res);
  const ids = body && body.errors && body.errors.conflictSeatIds;
  return Array.isArray(ids) && ids.length > 0;
}

export default function (data) {
  const i = exec.scenario.iterationInTest; // duy nhất, bắt đầu từ 0
  const showtime = data.showtimes[i % data.showtimes.length];
  const user = users[customerIdx[i % accountCount]];

  // 1. Login: user mới mỗi iteration (không dùng lại token). login() tự xử lý 429 bằng Retry-After.
  let session;
  try {
    session = login(user, {
      tags: { step: 'login' },
      onResponse: (res) => record('login', res, null),
    });
  } catch (e) {
    stepSuccess.add(false, { step: 'login' });
    logFailure(String(e.message || e));
    return;
  }
  stepSuccess.add(true, { step: 'login' });
  const headers = authHeaders(session.accessToken);

  // 2. Xem suất chiếu trong ngày của suất đang test.
  const listRes = http.get(`${API_URL}/showtimes?date=${showtime.date}`, {
    headers,
    tags: { name: NAMES.showtimes, step: 'showtimes' },
  });
  const listOk = checkOk(listRes, NAMES.showtimes);
  record('showtimes', listRes, listOk);
  if (!listOk) return;
  // Kiểm tra mềm (không tính vào tỉ lệ thành công): suất đang test có trong lịch hay không.
  check(listRes, {
    'showtimes: lịch có chứa suất đang test': (r) => {
      const movies = ((parseBody(r) || {}).data || {}).movies || [];
      return movies.some((m) => (m.showtimes || []).some((s) => s.showtimeId === showtime.id));
    },
  });

  // 3. Xem sơ đồ ghế.
  const seatsRes = http.get(`${API_URL}/showtimes/${showtime.id}/seats`, {
    headers,
    tags: { name: NAMES.seats, step: 'seats' },
  });
  const seatMap = (parseBody(seatsRes) || {}).data;
  const seatsOk =
    checkOk(seatsRes, NAMES.seats) && !!seatMap && Array.isArray(seatMap.seats) && seatMap.seats.length > 0;
  record('seats', seatsRes, seatsOk);
  if (!seatsOk) return;

  // 4. Chọn ghế theo số iteration (hoán vị cố định, không trùng giữa các iteration), đi tiếp trong
  //    hoán vị nếu ghế dự kiến đã không còn trống (ví dụ còn giữ lại từ lần chạy trước).
  const candidates = candidateSeats(seatMap.seats);
  let seat = null;
  if (candidates.length > 0) {
    const n = candidates.length;
    const slot = Math.floor(i / data.showtimes.length);
    const start = (slot * strideFor(n)) % n;
    for (let k = 0; k < n && seat === null; k++) {
      const c = candidates[(start + k) % n];
      if (c.status === 'Available') seat = c;
    }
  }
  if (seat === null) {
    stepSuccess.add(false, { step: 'hold' });
    logFailure(`Suất ${showtime.id} không còn ghế ứng viên nào còn trống.`);
    return;
  }

  const holdRes = http.post(
    `${API_URL}/bookings/hold`,
    JSON.stringify({
      showtimeId: showtime.id,
      customerName: `Loadtest ${i}`,
      seatIds: [seat.showtimeSeatId],
    }),
    { headers, tags: { name: NAMES.hold, step: 'hold' } },
  );
  // 409 tranh ghế (có errors.conflictSeatIds) là kết quả nghiệp vụ hợp lệ: đếm vào conflict_409,
  // không tính vào tỉ lệ thành công, kết thúc iteration. 409 khác (ví dụ đã có booking chờ) là lỗi thật.
  if (holdRes.status === 409 && hasConflictSeatIds(holdRes)) {
    checkOkOrConflict(holdRes, NAMES.hold, 201);
    record('hold', holdRes, null);
    return;
  }
  const holdOk = checkOk(holdRes, NAMES.hold, 201);
  const bookingId = holdOk ? ((parseBody(holdRes) || {}).data || {}).bookingId : undefined;
  record('hold', holdRes, holdOk && !!bookingId);
  if (!holdOk || !bookingId) return;

  // 5. Checkout booking vừa giữ.
  const checkoutRes = http.post(
    `${API_URL}/bookings/${bookingId}/checkout`,
    JSON.stringify({ paymentMethod: PAYMENT_METHOD }),
    { headers, tags: { name: NAMES.checkout, step: 'checkout' } },
  );
  const checkoutOk =
    checkOk(checkoutRes, NAMES.checkout) &&
    !!((parseBody(checkoutRes) || {}).data || {}).paymentUrl;
  record('checkout', checkoutRes, checkoutOk);
}
