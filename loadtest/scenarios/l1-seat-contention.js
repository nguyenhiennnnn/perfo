// L1 - Tranh ghế: 100 VU bắn đồng thời POST /bookings/hold cùng MỘT ghế, mỗi VU 1 lần.
// Kỳ vọng: đúng 1 VU nhận 201, 99 VU nhận 409, không có 5xx, p95 < 800ms.
//
// Chạy (chỉ staging/local):
//   k6 run -e BASE_URL=http://localhost:8080 -e SHOWTIME_ID=123 loadtest/scenarios/l1-seat-contention.js
//   thêm -e SEAT_ID=<showtimeSeatId> để chỉ định ghế; bỏ trống thì tự chọn một ghế còn trống ở giữa phòng.
//
// Điều kiện: >= 100 tài khoản khách trong loadtest/data/users.json (node loadtest/scripts/seed-users.mjs --yes)
// và đã nâng RateLimit (xem .env.loadtest.example), nếu không phần lớn request sẽ nhận 429.

import http from 'k6/http';
import exec from 'k6/execution';
import { check, sleep } from 'k6';
import { Counter } from 'k6/metrics';
import { API_URL } from '../lib/config.js';
import { users, login, authHeaders } from '../lib/auth.js';
import { checkOkOrConflict, parseBody } from '../lib/checks.js';

const VUS = 100;
const HOLD_NAME = 'POST /bookings/hold';
const SEATS_NAME = 'GET /showtimes/{id}/seats';
const HOLD_URL = `${API_URL}/bookings/hold`;
// Thời gian chờ (ms) sau setup để mọi VU đã sẵn sàng rồi mới bắn cùng một thời điểm.
const FIRE_DELAY_MS = 3000;

const SHOWTIME_ID = Number(__ENV.SHOWTIME_ID);
const SEAT_ID_OVERRIDE = __ENV.SEAT_ID ? Number(__ENV.SEAT_ID) : null;

// 201 (thắng ghế) và 409 (thua ghế) đều là kết quả mong đợi của kịch bản này, nên không tính vào
// http_req_failed. Chỉ áp cho request hold (per-request), không đụng tới login/GET seats trong setup.
const HOLD_EXPECTED = http.expectedStatuses(201, 409);

// 409 đếm bằng conflict_409 (lib/checks.js, qua checkOkOrConflict).
const holdSuccess = new Counter('hold_success');
const hold5xx = new Counter('hold_5xx');
const holdOther = new Counter('hold_other_status'); // 429, 4xx khác, lỗi mạng (status 0)

export const options = {
  setupTimeout: '10m', // đăng nhập tuần tự 100 tài khoản, có thể chờ khi gặp 429
  scenarios: {
    seat_contention: {
      executor: 'per-vu-iterations',
      vus: VUS,
      iterations: 1,
      maxDuration: '60s',
    },
  },
  thresholds: {
    hold_success: ['count==1'],
    conflict_409: [`count==${VUS - 1}`],
    hold_5xx: ['count==0'],
    [`http_req_duration{name:${HOLD_NAME}}`]: ['p(95)<800'],
    // 409 đã nằm trong expectedStatuses nên rate phải bằng 0; 429/4xx/lỗi mạng sẽ làm rate > 0.
    [`http_req_failed{name:${HOLD_NAME}}`]: ['rate==0'],
  },
};

function fail(message) {
  throw new Error(message);
}

// Chọn ghế thường còn trống ở giữa phòng, có đủ 2 ghế trống mỗi bên cùng hàng: tránh luật "ghế trống lẻ"
// (SeatLayoutRules.ValidateSelection) trả 422 thay vì 409 cho các VU thua.
function pickSeatId() {
  const res = http.get(`${API_URL}/showtimes/${SHOWTIME_ID}/seats`, { tags: { name: SEATS_NAME } });
  if (res.status !== 200) {
    fail(`Không lấy được sơ đồ ghế của suất ${SHOWTIME_ID}: HTTP ${res.status} ${res.body}`);
  }
  const body = parseBody(res);
  const data = body && body.data;
  if (!data || !Array.isArray(data.seats) || data.seats.length === 0) {
    fail(`Suất ${SHOWTIME_ID} không có ghế nào.`);
  }

  const byKey = {};
  for (const s of data.seats) byKey[`${s.seatRow}-${s.seatNumber}`] = s;
  const isFree = (row, n) => {
    const s = byKey[`${row}-${n}`];
    return !!s && s.status === 'Available';
  };

  const midRow = (data.rows || 1) / 2;
  const midCol = (data.cols || 1) / 2;
  let best = null;
  let bestScore = Infinity;
  for (const s of data.seats) {
    if (s.status !== 'Available' || s.isCouple || s.coupleId) continue;
    const row = s.seatRow;
    const n = s.seatNumber;
    if (!(isFree(row, n - 1) && isFree(row, n + 1) && isFree(row, n - 2) && isFree(row, n + 2))) continue;
    const rowIdx = row.charCodeAt(0) - 65 + 1;
    const score = Math.abs(rowIdx - midRow) + Math.abs(n - midCol);
    if (score < bestScore) {
      bestScore = score;
      best = s;
    }
  }
  if (!best) {
    fail(`Suất ${SHOWTIME_ID} không còn ghế thường nào có đủ 2 ghế trống mỗi bên. Dùng suất mới (seed-showtime --new) hoặc truyền -e SEAT_ID=...`);
  }
  console.log(`Ghế được chọn: ${best.seatRow}${best.seatNumber} (showtimeSeatId=${best.showtimeSeatId})`);
  return best.showtimeSeatId;
}

export function setup() {
  if (!Number.isInteger(SHOWTIME_ID) || SHOWTIME_ID <= 0) {
    fail('Thiếu SHOWTIME_ID. Chạy: k6 run -e SHOWTIME_ID=<id> ... (lấy từ seed-showtime.mjs).');
  }
  if (SEAT_ID_OVERRIDE !== null && (!Number.isInteger(SEAT_ID_OVERRIDE) || SEAT_ID_OVERRIDE <= 0)) {
    fail('SEAT_ID phải là số nguyên dương (showtimeSeatId).');
  }

  // Mỗi VU cần một tài khoản riêng. Dùng chung tài khoản sẽ nhận 409 "đã có booking chờ" chứ không phải
  // 409 tranh ghế, làm kết quả sai lệch.
  const accounts = [];
  for (let i = 0; i < users.length && accounts.length < VUS; i++) {
    if (users[i].portal !== 'admin') accounts.push(users[i]);
  }
  if (accounts.length < VUS) {
    fail(`Cần ít nhất ${VUS} tài khoản khách trong loadtest/data/users.json, hiện có ${accounts.length}. Chạy: node loadtest/scripts/seed-users.mjs --yes`);
  }

  const seatId = SEAT_ID_OVERRIDE !== null ? SEAT_ID_OVERRIDE : pickSeatId();

  // Đăng nhập trước, ngoài đo đạc, để lượt bắn chỉ gồm request hold.
  const tokens = [];
  for (let i = 0; i < VUS; i++) tokens.push(login(accounts[i]).accessToken);

  return { showtimeId: SHOWTIME_ID, seatId, tokens, fireAt: Date.now() + FIRE_DELAY_MS };
}

function hasConflictSeatIds(res) {
  const body = parseBody(res);
  const ids = body && body.errors && body.errors.conflictSeatIds;
  return Array.isArray(ids) && ids.length > 0;
}

export default function (data) {
  const vuIndex = exec.vu.idInTest - 1; // VU id bắt đầu từ 1
  const token = data.tokens[vuIndex];

  // Hàng rào thời gian: mọi VU chờ đến cùng một thời điểm rồi mới bắn.
  const wait = data.fireAt - Date.now();
  if (wait > 0) sleep(wait / 1000);

  const res = http.post(
    HOLD_URL,
    JSON.stringify({
      showtimeId: data.showtimeId,
      customerName: `Loadtest VU ${exec.vu.idInTest}`,
      seatIds: [data.seatId],
    }),
    { headers: authHeaders(token), tags: { name: HOLD_NAME }, responseCallback: HOLD_EXPECTED },
  );

  if (res.status === 201) holdSuccess.add(1);
  else if (res.status >= 500) hold5xx.add(1);
  else if (res.status !== 409) holdOther.add(1);

  // Đếm 409 vào conflict_409 và kiểm tra status 201/409 (chỉ gọi một lần cho mỗi response).
  checkOkOrConflict(res, HOLD_NAME, 201);
  check(res, {
    'hold 201: body có data.bookingId': (r) => {
      if (r.status !== 201) return true;
      const body = parseBody(r);
      return !!(body && body.data && body.data.bookingId);
    },
    'hold 409: là tranh ghế (có errors.conflictSeatIds)': (r) => r.status !== 409 || hasConflictSeatIds(r),
  });
}

function metricValue(data, name, stat) {
  const m = data.metrics[name];
  if (!m) return 0;
  const v = m.values || m;
  return v[stat] === undefined ? 0 : v[stat];
}

function failedThresholds(data) {
  const out = [];
  for (const name of Object.keys(data.metrics)) {
    const th = data.metrics[name].thresholds;
    if (!th) continue;
    for (const expr of Object.keys(th)) {
      const ok = typeof th[expr] === 'object' ? th[expr].ok : th[expr];
      if (!ok) out.push(`${name}  ${expr}`);
    }
  }
  return out;
}

export function handleSummary(data) {
  const success = metricValue(data, 'hold_success', 'count');
  const conflict = metricValue(data, 'conflict_409', 'count');
  const server5xx = metricValue(data, 'hold_5xx', 'count');
  const other = metricValue(data, 'hold_other_status', 'count');
  const p95 = metricValue(data, `http_req_duration{name:${HOLD_NAME}}`, 'p(95)');
  const failed = failedThresholds(data);

  const lines = [
    '',
    '=== L1 - Tranh ghế: kết quả ===',
    `  Tổng request hold          : ${success + conflict + server5xx + other} (kỳ vọng ${VUS})`,
    `  Thành công (201)           : ${success} (kỳ vọng 1)`,
    `  Xung đột (409)             : ${conflict} (kỳ vọng ${VUS - 1})`,
    `  Lỗi máy chủ (5xx)          : ${server5xx} (kỳ vọng 0)`,
    `  Khác (429/4xx/lỗi mạng)    : ${other} (kỳ vọng 0)`,
    `  p95 thời gian phản hồi hold: ${p95.toFixed(1)} ms (ngưỡng < 800 ms)`,
    `  Check đạt / hỏng           : ${metricValue(data, 'checks', 'passes')} / ${metricValue(data, 'checks', 'fails')}`,
    '',
  ];

  if (failed.length === 0) {
    lines.push('KẾT LUẬN: ĐẠT - mọi ngưỡng đều thỏa.');
  } else {
    lines.push('KẾT LUẬN: KHÔNG ĐẠT - ngưỡng thất bại:');
    for (const f of failed) lines.push(`  - ${f}`);
    if (success === 0 && conflict > 0) {
      lines.push('  Gợi ý: không VU nào giữ được ghế, ghế có thể đang bị giữ từ lần chạy trước (chờ hết hạn giữ chỗ hoặc đổi ghế/suất).');
    }
    if (other > 0) {
      lines.push('  Gợi ý: có phản hồi ngoài 201/409 (thường là 429 do rate limit; xem .env.loadtest.example).');
    }
  }
  lines.push('');

  return { stdout: lines.join('\n') };
}
