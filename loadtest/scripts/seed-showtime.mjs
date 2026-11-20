#!/usr/bin/env node
// Tạo 1 suất chiếu lớn (phòng >= 300 ghế) qua API admin và in showtimeId.
//
// Các bước (đều là API admin thật):
//   1. POST /auth/admin-login                      lấy token admin
//   2. GET  /admin/rooms                           tìm phòng "Loadtest Hall" (hoặc phòng bất kỳ >= 300 ghế)
//      POST /admin/rooms + PUT /admin/rooms/{id}/seats   nếu chưa có: tạo phòng 20 x 16 = 320 ghế thường
//   3. GET  /admin/movies                          chọn phim chưa kết thúc
//   4. GET  /admin/showtimes                       nếu đã có suất tương lai trong phòng thì dùng lại
//      POST /admin/showtimes                       nếu chưa có thì tạo mới
//
// Dùng:
//   node loadtest/scripts/seed-showtime.mjs [--yes] [--new] [--start=2026-11-21T12:00:00Z]
//        [--movie-id=N] [--price=90000] [--rows=20] [--cols=16]
// Biến môi trường: BASE_URL, ADMIN_EMAIL, ADMIN_PASSWORD, ROOM_NAME.

import { adminLogin, assertSafeTarget, BASE_URL, parseArgs, request } from './api-client.mjs';

const args = parseArgs(process.argv.slice(2));
assertSafeTarget(args);

const MIN_SEATS = 300;
const ROWS = Number(args.rows || 20);
const COLS = Number(args.cols || 16);
const PRICE = Number(args.price || 90000);
const ROOM_NAME = process.env.ROOM_NAME || 'Loadtest Hall 320';
const MAX_START_SHIFTS = 7;

if (!Number.isInteger(ROWS) || !Number.isInteger(COLS) || ROWS < 1 || ROWS > 26 || COLS < 1) {
  fail('--rows phải từ 1 đến 26 (hàng A-Z) và --cols phải là số nguyên dương.');
}
if (ROWS * COLS < MIN_SEATS) fail(`rows x cols = ${ROWS * COLS} < ${MIN_SEATS} ghế yêu cầu.`);

function fail(message) {
  console.error(message);
  process.exit(1);
}

// Thoát với thông báo lỗi của server nếu không phải 2xx.
function expectOk(res, what) {
  if (res.status < 200 || res.status >= 300) {
    const detail = res.body?.message ? `${res.body.message} ` : '';
    const errors = res.body?.errors ? JSON.stringify(res.body.errors) : '';
    fail(`${what} thất bại: HTTP ${res.status} ${detail}${errors || (res.body ? '' : res.text)}`);
  }
  return res.body?.data;
}

// Chuỗi thời gian từ server có thể thiếu hậu tố múi giờ: coi là UTC.
function parseUtc(value) {
  const text = String(value);
  return new Date(/(Z|[+-]\d\d:\d\d)$/.test(text) ? text : `${text}Z`);
}

const token = await adminLogin();
console.log(`Server: ${BASE_URL}`);

// ───────────────────────── Phòng ─────────────────────────

async function findOrCreateRoom() {
  const list = expectOk(await request('GET', '/admin/rooms?page=1&pageSize=100', { token }), 'Lấy danh sách phòng');
  const rooms = list.items;

  const named = rooms.find((r) => r.roomName === ROOM_NAME);
  const big = rooms.find((r) => r.isActive && r.totalSeats >= MIN_SEATS);
  let room = named ?? big;

  if (!room) {
    room = expectOk(
      await request('POST', '/admin/rooms', {
        token,
        body: { roomName: ROOM_NAME, screenTypeId: null, rows: ROWS, cols: COLS },
      }),
      'Tạo phòng',
    );
    console.log(`Đã tạo phòng #${room.roomId} "${room.roomName}"`);
  }

  if (!room.isActive) fail(`Phòng #${room.roomId} "${room.roomName}" đang ngừng hoạt động; bật lại hoặc đổi ROOM_NAME.`);
  if (room.totalSeats >= MIN_SEATS) return room;

  // Phòng chưa có sơ đồ ghế (vừa tạo, hoặc lần chạy trước dừng giữa chừng): dựng sơ đồ toàn ghế thường.
  const types = expectOk(await request('GET', '/admin/seat-types?page=1&pageSize=100', { token }), 'Lấy loại ghế');
  const regular = types.items.filter((t) => !t.isCouple);
  const seatType = regular.find((t) => t.name === 'Standard') ?? regular[0];
  if (!seatType) fail('Không có loại ghế không phải ghế đôi để dựng sơ đồ.');

  const seats = [];
  for (let r = 0; r < ROWS; r++) {
    for (let c = 1; c <= COLS; c++) {
      seats.push({
        seatRow: String.fromCharCode(65 + r),
        seatNumber: c,
        seatTypeId: seatType.seatTypeId,
        isActive: true,
        isAisle: false,
        coupleId: null,
      });
    }
  }

  const layout = expectOk(
    await request('PUT', `/admin/rooms/${room.roomId}/seats`, { token, body: { rows: ROWS, cols: COLS, seats } }),
    'Lưu sơ đồ ghế',
  );
  if (layout.totalSeats < MIN_SEATS) fail(`Phòng chỉ có ${layout.totalSeats} ghế sau khi lưu sơ đồ (cần >= ${MIN_SEATS}).`);
  console.log(`Đã dựng sơ đồ ${layout.totalSeats} ghế cho phòng #${room.roomId}`);
  return { ...room, totalSeats: layout.totalSeats };
}

// ───────────────────────── Phim ─────────────────────────

async function pickMovieId() {
  if (args['movie-id']) return Number(args['movie-id']);
  for (const status of ['NowShowing', 'ComingSoon']) {
    const list = expectOk(
      await request('GET', `/admin/movies?status=${status}&page=1&pageSize=100`, { token }),
      'Lấy danh sách phim',
    );
    if (list.items.length > 0) return list.items[0].movieId;
  }
  return fail('Không có phim nào ở trạng thái NowShowing/ComingSoon. Truyền --movie-id=N.');
}

// ───────────────────────── Suất chiếu ─────────────────────────

const room = await findOrCreateRoom();

if (!args.new) {
  const existing = expectOk(
    await request('GET', `/admin/showtimes?roomId=${room.roomId}&status=Scheduled&page=1&pageSize=100`, { token }),
    'Lấy suất chiếu hiện có',
  );
  const soonest = Date.now() + 60 * 60 * 1000;
  const reusable = existing.items
    .filter((s) => parseUtc(s.startTime).getTime() > soonest)
    .sort((a, b) => parseUtc(a.startTime) - parseUtc(b.startTime))[0];
  if (reusable) {
    report(reusable.showtimeId, room, reusable.startTime, 'dùng lại suất đã có (thêm --new để tạo suất mới)');
    process.exit(0);
  }
}

const movieId = await pickMovieId();

// Mặc định: ngày mai 12:00 UTC (= 19:00 giờ Việt Nam), nằm trong giờ mở cửa 08:00-23:59 của seed.
// Nếu đã có suất chồng giờ trong phòng (409) thì dời sang ngày kế tiếp.
const base = new Date();
let start = args.start
  ? new Date(String(args.start))
  : new Date(Date.UTC(base.getUTCFullYear(), base.getUTCMonth(), base.getUTCDate() + 1, 12, 0, 0));
if (Number.isNaN(start.getTime())) fail('--start không phải thời điểm ISO hợp lệ (ví dụ 2026-11-21T12:00:00Z).');

for (let attempt = 0; attempt <= MAX_START_SHIFTS; attempt++) {
  const res = await request('POST', '/admin/showtimes', {
    token,
    body: { movieId, roomId: room.roomId, startTime: start.toISOString(), basePrice: PRICE, allowAfterHours: false },
  });

  if (res.status === 201 || res.status === 200) {
    report(res.body.data.showtimeId, room, res.body.data.startTime ?? start.toISOString(), 'đã tạo mới');
    process.exit(0);
  }
  if (res.status === 409 && !args.start) {
    start = new Date(start.getTime() + 24 * 60 * 60 * 1000);
    continue;
  }
  expectOk(res, `Tạo suất chiếu (${start.toISOString()})`);
}
fail(`Phòng vẫn bị chồng giờ sau ${MAX_START_SHIFTS} lần dời ngày. Truyền --start=... cụ thể.`);

function report(showtimeId, roomInfo, startTime, note) {
  console.log(`\nSuất chiếu: ${note}`);
  console.log(`  phòng     : #${roomInfo.roomId} "${roomInfo.roomName}" (${roomInfo.totalSeats} ghế)`);
  console.log(`  bắt đầu   : ${startTime}`);
  console.log(`  showtimeId: ${showtimeId}`);
  console.log(`\nSHOWTIME_ID=${showtimeId}`);
}
