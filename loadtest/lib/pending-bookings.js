// Tạo sẵn N booking Pending kèm payment Pending (đã checkout) để kịch bản gửi IPN vào. Chỉ dùng trong setup().
//
// Mỗi booking: 1 ghế thường, 1 payment VNPay. Luồng: login khách -> hold (POST /bookings/hold)
// -> checkout (POST /bookings/{id}/checkout, VNPay chỉ tự ký URL, không gọi cổng ngoài).
//
// Chọn ghế/tài khoản (tránh xung đột):
//   * Ghế ứng viên giống L2: số ghế có (seatNumber - 1) chia hết cho 3, không phải ghế đôi, không tạo "ghế trống lẻ"
//     (SeatLayoutRules chế độ Strict trả 422). Chỉ lấy ghế đang Available nên ghế còn bị giữ từ lần chạy trước tự bị bỏ qua.
//   * Backend chỉ cho mỗi user MỘT booking Pending còn hiệu lực trên mỗi suất (409 "đã có booking chờ"). Vì vậy
//     vòng r (r = 0, 1, ...) dùng tài khoản khách thứ r, mỗi suất một ghế: số booking tối đa mỗi suất = min(số ghế
//     ứng viên còn trống, số tài khoản khách).
//   * Giữ ghế hết hạn sau HoldDurationMinutes (mặc định 15 phút). Hàm trả expiresAtMs nhỏ nhất để kịch bản kiểm tra.
//
// Điều kiện: đã nâng RateLimit (Global, Auth, BookingHold; xem .env.loadtest.example).

import http from 'k6/http';
import { API_URL } from './config.js';
import { users, login, authHeaders } from './auth.js';
import { parseBody } from './checks.js';
import { parsePaymentUrl } from './ipn.js';

const SEAT_SPACING = 3;
const MAX_ERROR_SAMPLES = 3;

const NAMES = {
  seats: 'GET /showtimes/{id}/seats',
  hold: 'POST /bookings/hold',
  checkout: 'POST /bookings/{id}/checkout',
};

function fail(message) {
  throw new Error(message);
}

// Ghế ứng viên theo cấu trúc sơ đồ, thứ tự ổn định (cùng quy tắc l2-peak-booking.js).
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

// API trả thời điểm UTC; chuỗi thiếu múi giờ coi là UTC.
function parseUtcMs(text) {
  const s = String(text);
  return Date.parse(/(Z|[+-]\d\d:\d\d)$/.test(s) ? s : `${s}Z`);
}

// Chạy các request theo từng nhóm http.batch. build(item, index) -> [method, url, body, params];
// handle(item, index, res) xử lý từng response. Cần options.batch / options.batchPerHost >= batchSize.
function runBatches(items, batchSize, build, handle) {
  for (let i = 0; i < items.length; i += batchSize) {
    const chunk = items.slice(i, i + batchSize);
    const responses = http.batch(chunk.map((item, j) => build(item, i + j)));
    for (let j = 0; j < chunk.length; j++) handle(chunk[j], i + j, responses[j]);
  }
}

// options: { showtimeIds, count, paymentMethod = 'VNPay', batchSize = 25, namePrefix = 'Loadtest IPN' }
// Trả { bookings: [{ bookingId, txnRef, amountVnp, expiresAtMs }], minExpiresAtMs }; ném lỗi nếu không đủ `count`.
export function createPendingBookings(options) {
  const showtimeIds = options.showtimeIds;
  const count = options.count;
  const paymentMethod = options.paymentMethod || 'VNPay';
  const batchSize = options.batchSize || 25;
  const namePrefix = options.namePrefix || 'Loadtest IPN';

  // Tài khoản khách (bỏ admin), lấy theo chỉ số để không sao chép dữ liệu dùng chung.
  const customers = [];
  for (let i = 0; i < users.length; i++) if (users[i].portal !== 'admin') customers.push(users[i]);
  if (customers.length === 0) fail('Không có tài khoản khách nào trong loadtest/data/users.json.');

  // 1. Sơ đồ ghế từng suất -> ghế ứng viên còn trống.
  const perShowtime = [];
  let capacity = 0;
  for (const id of showtimeIds) {
    const res = http.get(`${API_URL}/showtimes/${id}/seats`, { tags: { name: NAMES.seats, step: 'setup' } });
    const seats = res.status === 200 ? ((parseBody(res) || {}).data || {}).seats : null;
    if (!Array.isArray(seats) || seats.length === 0) {
      fail(`Không lấy được sơ đồ ghế của suất ${id}: HTTP ${res.status} ${res.body}`);
    }
    const free = candidateSeats(seats).filter((s) => s.status === 'Available');
    const usable = free.slice(0, customers.length);
    perShowtime.push({ id, seats: usable });
    capacity += usable.length;
    console.log(`Suất ${id}: ${free.length} ghế ứng viên còn trống (dùng tối đa ${usable.length})`);
  }
  if (capacity < count) {
    fail(
      `Không đủ chỗ: cần ${count} booking Pending, nhưng ${showtimeIds.length} suất chỉ cho ${capacity} ` +
        `(mỗi suất tối đa min(ghế ứng viên còn trống, ${customers.length} tài khoản khách)). ` +
        'Thêm suất chiếu mới (seed-showtime.mjs --new --start=...), thêm tài khoản (seed-users.mjs) hoặc giảm tải.',
    );
  }

  // 2. Kế hoạch: vòng r dùng tài khoản r, mỗi suất một ghế.
  const plan = [];
  for (let round = 0; plan.length < count; round++) {
    for (const st of perShowtime) {
      if (plan.length < count && round < st.seats.length) {
        plan.push({ showtimeId: st.id, seat: st.seats[round], userIdx: round });
      }
    }
  }
  const usersNeeded = plan[plan.length - 1].userIdx + 1;

  // 3. Đăng nhập các tài khoản cần dùng (tuần tự; login() tự xử lý 429 bằng Retry-After).
  const tokens = [];
  for (let u = 0; u < usersNeeded; u++) tokens.push(login(customers[u]).accessToken);
  console.log(`Đã đăng nhập ${usersNeeded} tài khoản khách; tạo ${count} booking Pending...`);

  // 4. Hold.
  const held = [];
  let holdFailures = 0;
  const holdErrors = [];
  runBatches(
    plan,
    batchSize,
    (p, i) => [
      'POST',
      `${API_URL}/bookings/hold`,
      JSON.stringify({ showtimeId: p.showtimeId, customerName: `${namePrefix} ${i}`, seatIds: [p.seat.showtimeSeatId] }),
      { headers: authHeaders(tokens[p.userIdx]), tags: { name: NAMES.hold, step: 'setup' } },
    ],
    (p, i, res) => {
      const data = res.status === 201 ? (parseBody(res) || {}).data : null;
      if (!data || !data.bookingId) {
        holdFailures++;
        if (holdErrors.length < MAX_ERROR_SAMPLES) holdErrors.push(`suất ${p.showtimeId}: HTTP ${res.status} ${res.body}`);
        return;
      }
      held.push({ bookingId: data.bookingId, userIdx: p.userIdx, expiresAtMs: parseUtcMs(data.expiresAt) });
    },
  );
  if (holdFailures > 0) {
    fail(`Giữ ghế thất bại ${holdFailures}/${count} (ví dụ: ${holdErrors.join(' | ')}). Kiểm tra RateLimit BookingHold và ghế đã bị giữ.`);
  }

  // 5. Checkout -> payment Pending + vnp_TxnRef/vnp_Amount.
  const bookings = [];
  let checkoutFailures = 0;
  const checkoutErrors = [];
  runBatches(
    held,
    batchSize,
    (h) => [
      'POST',
      `${API_URL}/bookings/${h.bookingId}/checkout`,
      JSON.stringify({ paymentMethod }),
      { headers: authHeaders(tokens[h.userIdx]), tags: { name: NAMES.checkout, step: 'setup' } },
    ],
    (h, i, res) => {
      const paymentUrl = res.status === 200 ? (((parseBody(res) || {}).data || {}).paymentUrl) : null;
      if (!paymentUrl) {
        checkoutFailures++;
        if (checkoutErrors.length < MAX_ERROR_SAMPLES) checkoutErrors.push(`booking ${h.bookingId}: HTTP ${res.status} ${res.body}`);
        return;
      }
      const parsed = parsePaymentUrl(paymentUrl);
      bookings.push({
        bookingId: h.bookingId,
        txnRef: parsed.txnRef,
        amountVnp: parsed.amountVnp,
        expiresAtMs: h.expiresAtMs,
      });
    },
  );
  if (checkoutFailures > 0) {
    fail(`Checkout thất bại ${checkoutFailures}/${held.length} (ví dụ: ${checkoutErrors.join(' | ')}).`);
  }

  let minExpiresAtMs = Infinity;
  for (const b of bookings) if (b.expiresAtMs < minExpiresAtMs) minExpiresAtMs = b.expiresAtMs;
  return { bookings, minExpiresAtMs };
}
