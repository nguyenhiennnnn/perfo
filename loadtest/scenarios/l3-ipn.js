// L3 - IPN VNPay dồn dập, có IPN gửi trùng. Kiểm tra: webhook không chậm và không xác nhận booking hai lần.
//
// Tải: constant-arrival-rate theo 3 bậc nối tiếp 50 -> 75 -> 100 req/s, mỗi bậc 1 phút (tổng 3 phút, 13.500 request).
// (constant-arrival-rate chỉ giữ MỘT tốc độ cố định nên "50 -> 100" được chia thành các bậc; mỗi bậc là một
// scenario riêng. Đổi bằng IPN_RATES / STEP_DURATION.)
// Mỗi iteration gửi đúng MỘT IPN (GET /api/v1/payments/vnpay/ipn?...&vnp_SecureHash=...):
//   * cứ DUP_EVERY (mặc định 5) request thì 1 request là IPN TRÙNG (~20%): bản gửi lại y hệt của một IPN đã/đang gửi.
//     Các IPN trùng xen kẽ: một nửa trùng với IPN ngay trước đó (hai request gần như đồng thời, thử race của
//     PaymentService.ProcessVnPayIpnAsync), phần còn lại trùng với IPN cũ hơn 4 và 40 vị trí.
//   * các request còn lại là IPN gốc cho một booking Pending riêng, ký HMAC-SHA512 như VNPay thật (lib/ipn.js).
//
// setup():    tạo sẵn đủ booking Pending (hold + checkout) cho mọi IPN gốc sẽ gửi (lib/pending-bookings.js).
// teardown(): đăng nhập admin, GET /api/v1/admin/bookings/{id} cho mọi booking đã tạo, kiểm tra không booking nào
//             Confirmed hai lần: Confirmed mà số vé > số ghế, vé trùng ghế, hoặc > 1 payment Success.
//
// Ngưỡng (xem options.thresholds):
//   - ipn_duration p(95) < 500ms (tất cả; cũng riêng cho từng loại original / duplicate)
//   - ipn_dup_mismatch: count < 1   IPN trùng PHẢI trả đúng kết quả của IPN gốc: HTTP 200 + { rspCode "00", message "Confirm Success" }
//   - ipn_original_mismatch: count < 1   IPN gốc cũng phải trả đúng kết quả đó (nếu không, "giống nhau" vô nghĩa)
//   - ipn_double_confirmed: count < 1    kết quả kiểm tra ở teardown
//   - ipn_verify_errors: count < 1       teardown không kiểm tra được (đăng nhập admin / GET chi tiết lỗi)
//
// Chạy (chỉ staging/local):
//   k6 run -e BASE_URL=http://localhost:8080 -e VNPAY_HASH_SECRET=<VnPay:HashSecret của backend> \
//          -e SHOWTIME_IDS=101,102,... loadtest/scenarios/l3-ipn.js
//   Chạy thử nhanh: thêm -e IPN_RATES=5,10 -e STEP_DURATION=20s
//
// Biến môi trường riêng của kịch bản:
//   VNPAY_HASH_SECRET  BẮT BUỘC, trùng VnPay:HashSecret của backend (VNPAY_TMN_CODE tùy chọn)
//   SHOWTIME_IDS       BẮT BUỘC, danh sách showtimeId cách nhau dấu phẩy (hoặc SHOWTIME_ID). Cần đủ chỗ cho
//                      ~10.800 booking: mỗi suất tối đa min(số ghế ứng viên còn trống, số tài khoản khách), xem
//                      lib/pending-bookings.js. Suất mới, chưa có ghế bị giữ/bán.
//   ADMIN_EMAIL, ADMIN_PASSWORD   tài khoản admin cho teardown (mặc định admin@cinema.local / Admin@123, như seed-showtime.mjs)
//   IPN_RATES          các bậc req/s (mặc định 50,75,100)
//   STEP_DURATION      thời lượng mỗi bậc (mặc định 1m)
//   DUP_EVERY          1 trong DUP_EVERY request là IPN trùng (mặc định 5 = 20%)
//   PRE_VUS / MAX_VUS  VU cấp sẵn / tối đa cho MỖI bậc (mặc định 50 / 200)
//   BATCH_SIZE         số request song song khi setup/teardown (mặc định 25)
//
// Điều kiện: đã nâng RateLimit Global (và Auth, BookingHold cho setup) theo .env.loadtest.example. Booking giữ ghế
// 15 phút (CinemaSettings.HoldDurationMinutes) nên setup + 3 phút test phải xong trong khoảng đó: setup() kiểm tra
// và dừng nếu không đủ. Chạy xong, các booking đã Confirmed: lần sau dùng suất chiếu mới.

import http from 'k6/http';
import exec from 'k6/execution';
import { check } from 'k6';
import { Counter, Trend } from 'k6/metrics';
import { API_URL } from '../lib/config.js';
import { login, authHeaders } from '../lib/auth.js';
import { parseBody, trackStatus } from '../lib/checks.js';
import {
  EXPECTED_RSP_CODE,
  EXPECTED_RSP_MESSAGE,
  VNPAY_HASH_SECRET,
  buildIpnParams,
  formatVnPayDate,
  ipnUrl,
  isExpectedIpnResponse,
  parseIpnResponse,
} from '../lib/ipn.js';
import { createPendingBookings } from '../lib/pending-bookings.js';

// ───────────────────────── Cấu hình ─────────────────────────

const RATES = String(__ENV.IPN_RATES || '50,75,100')
  .split(',')
  .map((s) => Number(s.trim()))
  .filter((n) => n > 0);
const STEP_DURATION = __ENV.STEP_DURATION || '1m';
const DUP_EVERY = Number(__ENV.DUP_EVERY || 5);
const PRE_VUS = Number(__ENV.PRE_VUS || 50);
const MAX_VUS = Number(__ENV.MAX_VUS || 200);
const BATCH_SIZE = Number(__ENV.BATCH_SIZE || 25);
const ADMIN_EMAIL = __ENV.ADMIN_EMAIL || 'admin@cinema.local';
const ADMIN_PASSWORD = __ENV.ADMIN_PASSWORD || 'Admin@123';

const SHOWTIME_IDS = String(__ENV.SHOWTIME_IDS || __ENV.SHOWTIME_ID || '')
  .split(',')
  .map((s) => s.trim())
  .filter((s) => s !== '')
  .map(Number);

const P95_LIMIT_MS = 500;
const IPN_NAME = 'GET /payments/vnpay/ipn';
const ADMIN_DETAIL_NAME = 'GET /admin/bookings/{id}';
const TRANSACTION_NO_BASE = 700000000; // vnp_TransactionNo = base + bookingId (ổn định giữa các lần gửi lại)
const EXPIRY_MARGIN_MS = 30 * 1000;

// Khoảng cách (số IPN gốc) giữa IPN trùng và IPN gốc mà nó gửi lại; 0 = ngay IPN gốc liền trước (race).
const DUP_LAGS = [0, 0, 4, 40];

function fail(message) {
  throw new Error(message);
}

function toSeconds(text) {
  const m = /^(\d+(?:\.\d+)?)(s|m|h)$/.exec(String(text).trim());
  if (!m) fail(`Khoảng thời gian không hợp lệ: "${text}" (ví dụ 30s, 1m).`);
  return Number(m[1]) * { s: 1, m: 60, h: 3600 }[m[2]];
}

const STEP_SECONDS = toSeconds(STEP_DURATION);

// Mỗi bậc là một scenario constant-arrival-rate; bậc sau bắt đầu khi bậc trước kết thúc.
// OFFSETS[tên scenario] = số chỉ số đã dành cho các bậc trước, để mọi request có chỉ số toàn cục g duy nhất.
// k6 có thể chạy dư 1 iteration mỗi bậc (rate x duration + 1), nên mỗi bậc dành thêm 1 chỉ số.
const scenarios = {};
const OFFSETS = {};
let plannedRequests = 0; // tổng request theo kế hoạch
let totalIterations = 0; // tổng chỉ số đã dành (>= plannedRequests), dùng để tính số booking cần tạo
RATES.forEach((rate, i) => {
  const name = `ipn_step_${i + 1}_${rate}rps`;
  scenarios[name] = {
    executor: 'constant-arrival-rate',
    rate,
    timeUnit: '1s',
    duration: STEP_DURATION,
    startTime: `${i * STEP_SECONDS}s`,
    preAllocatedVUs: PRE_VUS,
    maxVUs: MAX_VUS,
    gracefulStop: '30s',
  };
  OFFSETS[name] = totalIterations;
  plannedRequests += rate * STEP_SECONDS;
  totalIterations += rate * STEP_SECONDS + 1;
});
const TEST_SECONDS = RATES.length * STEP_SECONDS;

// ───────────────────────── Metric ─────────────────────────

// Thời gian phản hồi IPN (ms), gắn tag kind = original | duplicate.
const ipnDuration = new Trend('ipn_duration', true);

// Số IPN trả khác { 200, "00", "Confirm Success" }, tách theo loại.
const dupMismatch = new Counter('ipn_dup_mismatch');
const originalMismatch = new Counter('ipn_original_mismatch');

// Chỉ số IPN rơi ngoài số booking đã tạo (không nên xảy ra).
const poolExhausted = new Counter('ipn_pool_exhausted');

// Kết quả teardown.
const doubleConfirmed = new Counter('ipn_double_confirmed');
const verifyErrors = new Counter('ipn_verify_errors');

// ───────────────────────── Options ─────────────────────────

export const options = {
  setupTimeout: __ENV.SETUP_TIMEOUT || '15m',
  teardownTimeout: __ENV.TEARDOWN_TIMEOUT || '10m',
  // http.batch mặc định chỉ chạy 20 request song song và 6/host: nâng để setup/teardown đủ nhanh.
  batch: Math.max(BATCH_SIZE, 20),
  batchPerHost: Math.max(BATCH_SIZE, 6),
  scenarios,
  thresholds: {
    ipn_duration: [`p(95)<${P95_LIMIT_MS}`],
    // Sub-metric theo tag chỉ được k6 theo dõi khi có threshold.
    'ipn_duration{kind:original}': [`p(95)<${P95_LIMIT_MS}`],
    'ipn_duration{kind:duplicate}': [`p(95)<${P95_LIMIT_MS}`],
    ipn_dup_mismatch: ['count<1'],
    ipn_original_mismatch: ['count<1'],
    ipn_pool_exhausted: ['count<1'],
    ipn_double_confirmed: ['count<1'],
    ipn_verify_errors: ['count<1'],
  },
};

// ───────────────────────── Kế hoạch IPN ─────────────────────────

// Số IPN gốc (cần số booking Pending tương ứng) trong `total` request đầu.
function originalCount(total) {
  return total - Math.floor(total / DUP_EVERY);
}

// Request thứ g (từ 0): IPN trùng nếu g % DUP_EVERY == DUP_EVERY - 1. index = chỉ số booking trong pool.
//   IPN gốc thứ k dùng booking k; IPN trùng gửi lại booking của một IPN gốc đã phát trước đó theo DUP_LAGS.
function planFor(g) {
  if (g % DUP_EVERY !== DUP_EVERY - 1) {
    return { kind: 'original', index: g - Math.floor(g / DUP_EVERY) };
  }
  const dupNumber = Math.floor(g / DUP_EVERY); // số IPN trùng đã phát trước g
  const originalsBefore = g - dupNumber;
  const lag = DUP_LAGS[dupNumber % DUP_LAGS.length];
  return { kind: 'duplicate', index: Math.max(0, originalsBefore - 1 - lag) };
}

// ───────────────────────── setup ─────────────────────────

export function setup() {
  if (!(DUP_EVERY >= 2)) fail('DUP_EVERY phải >= 2.');
  if (RATES.length === 0) fail('IPN_RATES không hợp lệ (ví dụ 50,75,100).');
  if (!VNPAY_HASH_SECRET) {
    fail('Thiếu VNPAY_HASH_SECRET. Chạy: k6 run -e VNPAY_HASH_SECRET=<VnPay:HashSecret của backend> ...');
  }
  if (SHOWTIME_IDS.length === 0) {
    fail('Thiếu SHOWTIME_IDS. Chạy: k6 run -e SHOWTIME_IDS=101,102,... (lấy từ seed-showtime.mjs).');
  }
  for (const id of SHOWTIME_IDS) {
    if (!Number.isInteger(id) || id <= 0) fail(`showtimeId không hợp lệ: ${id}`);
  }
  if (new Set(SHOWTIME_IDS).size !== SHOWTIME_IDS.length) fail('SHOWTIME_IDS có id bị trùng.');

  const needed = originalCount(totalIterations);
  console.log(`Tải: ${plannedRequests} request (${RATES.join('/')} req/s x ${STEP_DURATION}); cần ${needed} booking Pending.`);
  const startedAt = Date.now();

  const { bookings, minExpiresAtMs } = createPendingBookings({
    showtimeIds: SHOWTIME_IDS,
    count: needed,
    paymentMethod: 'VNPay',
    batchSize: BATCH_SIZE,
  });

  // Giữ ghế hết hạn thì IPN không xác nhận được booking nữa (SeatsNoLongerHeld, hoàn tiền tự động) -> dừng sớm.
  const remainingMs = minExpiresAtMs - Date.now();
  const requiredMs = TEST_SECONDS * 1000 + EXPIRY_MARGIN_MS;
  console.log(
    `Setup xong sau ${Math.round((Date.now() - startedAt) / 1000)}s; booking hết hạn sớm nhất sau ${Math.round(remainingMs / 1000)}s, ` +
      `test cần ${Math.round(requiredMs / 1000)}s.`,
  );
  if (remainingMs < requiredMs) {
    fail(
      'Booking Pending hết hạn trước khi test xong (setup quá lâu hoặc lệch đồng hồ giữa máy chạy k6 và server). ' +
        'Giảm tải (IPN_RATES/STEP_DURATION) hoặc tăng CinemaSettings.HoldDurationMinutes trên staging, rồi dùng suất chiếu mới.',
    );
  }

  return {
    payDate: formatVnPayDate(new Date()),
    // Dạng gọn [bookingId, txnRef, amountVnp]: dữ liệu setup được sao chép cho từng VU.
    bookings: bookings.map((b) => [b.bookingId, b.txnRef, b.amountVnp]),
  };
}

// ───────────────────────── Luồng một IPN ─────────────────────────

let loggedFailures = 0; // giới hạn log lỗi theo VU

export default function (data) {
  const g = OFFSETS[exec.scenario.name] + exec.scenario.iterationInTest;
  const plan = planFor(g);
  const booking = data.bookings[plan.index];
  if (!booking) {
    poolExhausted.add(1);
    return;
  }

  const params = buildIpnParams({
    txnRef: booking[1],
    amountVnp: booking[2],
    transactionNo: TRANSACTION_NO_BASE + booking[0],
    payDate: data.payDate,
  });
  const res = http.get(ipnUrl(params), { tags: { name: IPN_NAME, kind: plan.kind } });

  ipnDuration.add(res.timings.duration, { kind: plan.kind });
  trackStatus(res, IPN_NAME); // đếm 429 (rate_limited_429) để phân biệt với lỗi thật

  const ok = isExpectedIpnResponse(res);
  check(
    res,
    { [`ipn ${plan.kind}: 200 + ${EXPECTED_RSP_CODE} "${EXPECTED_RSP_MESSAGE}"`]: () => ok },
    { kind: plan.kind },
  );
  if (!ok) {
    (plan.kind === 'duplicate' ? dupMismatch : originalMismatch).add(1);
    if (loggedFailures++ < 3) {
      const rsp = parseIpnResponse(res);
      console.error(
        `IPN ${plan.kind} booking ${booking[0]}: HTTP ${res.status} ` +
          (rsp ? `rspCode=${rsp.rspCode} message=${rsp.message}` : `body=${String(res.body).slice(0, 200)}`),
      );
    }
  }
}

// ───────────────────────── teardown ─────────────────────────

// true nếu booking Confirmed mà có dấu hiệu bị xác nhận hơn một lần.
function isDoubleConfirmed(detail) {
  if (String(detail.status).toLowerCase() !== 'confirmed') return false;
  const seats = detail.seats || [];
  const tickets = detail.tickets || [];
  const payments = detail.payments || [];
  const ticketSeats = new Set(tickets.map((t) => `${t.seatRow}${t.seatNumber}`));
  const successPayments = payments.filter((p) => String(p.status).toLowerCase() === 'success').length;
  return tickets.length > seats.length || ticketSeats.size < tickets.length || successPayments > 1;
}

export function teardown(data) {
  // Luôn ghi mẫu (kể cả 0) để threshold count < 1 có dữ liệu.
  doubleConfirmed.add(0);
  verifyErrors.add(0);

  let adminToken;
  try {
    adminToken = login({ email: ADMIN_EMAIL, password: ADMIN_PASSWORD, portal: 'admin' }).accessToken;
  } catch (e) {
    verifyErrors.add(1);
    console.error(`Teardown: đăng nhập admin thất bại, không kiểm tra được: ${e.message || e}`);
    return;
  }
  const headers = authHeaders(adminToken);

  const ids = data.bookings.map((b) => b[0]);
  const byStatus = {};
  const doubles = [];
  let errors = 0;

  for (let i = 0; i < ids.length; i += BATCH_SIZE) {
    const chunk = ids.slice(i, i + BATCH_SIZE);
    const responses = http.batch(
      chunk.map((id) => [
        'GET',
        `${API_URL}/admin/bookings/${id}`,
        null,
        { headers, tags: { name: ADMIN_DETAIL_NAME, step: 'teardown' } },
      ]),
    );
    for (let j = 0; j < chunk.length; j++) {
      const detail = responses[j].status === 200 ? (parseBody(responses[j]) || {}).data : null;
      if (!detail) {
        errors++;
        if (errors <= 3) console.error(`Teardown: GET booking ${chunk[j]} -> HTTP ${responses[j].status} ${responses[j].body}`);
        continue;
      }
      const status = String(detail.status);
      byStatus[status] = (byStatus[status] || 0) + 1;
      if (isDoubleConfirmed(detail)) doubles.push(chunk[j]);
    }
  }

  if (errors > 0) verifyErrors.add(errors);
  if (doubles.length > 0) {
    doubleConfirmed.add(doubles.length);
    console.error(`Teardown: ${doubles.length} booking Confirmed nhiều lần (ví dụ bookingId: ${doubles.slice(0, 10).join(', ')}).`);
  }
  console.log(
    `Teardown: đã kiểm tra ${ids.length - errors}/${ids.length} booking; trạng thái ${JSON.stringify(byStatus)}; ` +
      `Confirmed nhiều lần: ${doubles.length}.`,
  );
}
