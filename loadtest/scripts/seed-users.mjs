#!/usr/bin/env node
// Tạo tài khoản khách cho load test qua API đăng ký và ghi loadtest/data/users.json.
//
// Vì sao không chỉ gọi /auth/register: tài khoản mới có EmailConfirmed = false và login bị từ chối (403)
// cho tới khi xác thực email. Token xác thực chỉ được gửi trong email, API không trả về. Hai cách xác thực:
//
//   --confirm=mock (mặc định)  Script mở một "hộp thư giả" (giống Resend) để bắt email, lấy token rồi gọi
//                              POST /auth/confirm-email. Backend phải trỏ email vào đó:
//                                Email__ResendApiKey=loadtest-fake-key
//                                Email__ResendApiUrl=http://<địa-chỉ-máy-chạy-script>:9099/emails
//                              (backend phải kết nối được tới máy này; staging ở xa thì cần tunnel hoặc dùng sql).
//   --confirm=sql              Chỉ đăng ký, rồi in câu UPDATE để bạn tự chạy trên DB staging.
//
// Dùng:
//   node loadtest/scripts/seed-users.mjs [--count=500] [--confirm=mock|sql] [--concurrency=5] [--yes]
// Biến môi trường: BASE_URL, LOADTEST_PASSWORD, EMAIL_DOMAIN, MOCK_MAIL_PORT.

import { createServer } from 'node:http';
import { writeFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { assertSafeTarget, BASE_URL, parseArgs, request, sleep } from './api-client.mjs';

const args = parseArgs(process.argv.slice(2));
assertSafeTarget(args);

const COUNT = Number(args.count || 500);
const CONFIRM_MODE = args.confirm || 'mock';
const CONCURRENCY = Number(args.concurrency || 5);
const PASSWORD = process.env.LOADTEST_PASSWORD || 'Loadtest@123'; // đạt StrongPassword: hoa, thường, số, >= 8
const EMAIL_DOMAIN = process.env.EMAIL_DOMAIN || 'cinema.local';
const MOCK_PORT = Number(process.env.MOCK_MAIL_PORT || 9099);
const TOKEN_WAIT_MS = 90_000; // email đi qua Hangfire nên có thể trễ
const USERS_FILE = fileURLToPath(new URL('../data/users.json', import.meta.url));

if (!Number.isInteger(COUNT) || COUNT < 1) fail('--count phải là số nguyên dương.');
if (!['mock', 'sql'].includes(CONFIRM_MODE)) fail('--confirm chỉ nhận mock hoặc sql.');

const pad = Math.max(3, String(COUNT).length);
const emailOf = (i) => `loadtest.user${String(i).padStart(pad, '0')}@${EMAIL_DOMAIN}`;

function fail(message) {
  console.error(message);
  process.exit(1);
}

// ───────────────────────── Hộp thư giả (chỉ dùng ở --confirm=mock) ─────────────────────────

const inbox = new Map(); // email (chữ thường) -> { token, at }
const TOKEN_PATTERN = /auth\/confirm-email\?token=([A-Za-z0-9_-]+)/;

function startMailbox() {
  const server = createServer((req, res) => {
    const chunks = [];
    req.on('data', (c) => chunks.push(c));
    req.on('end', () => {
      let payload = {};
      try {
        payload = JSON.parse(Buffer.concat(chunks).toString('utf8'));
      } catch {
        /* bỏ qua body hỏng */
      }
      if (req.method === 'POST' && (req.url || '').split('?')[0] === '/emails') {
        const token = TOKEN_PATTERN.exec(String(payload.html ?? ''))?.[1];
        const recipients = Array.isArray(payload.to) ? payload.to : [payload.to];
        if (token) {
          for (const to of recipients) inbox.set(String(to).toLowerCase(), { token, at: Date.now() });
        }
        res.writeHead(200, { 'Content-Type': 'application/json' });
        res.end(JSON.stringify({ id: `mock-${Date.now()}` }));
        return;
      }
      res.writeHead(404, { 'Content-Type': 'application/json' });
      res.end('{}');
    });
  });

  return new Promise((resolve, reject) => {
    server.once('error', reject);
    server.listen(MOCK_PORT, () => resolve(server));
  });
}

async function waitForToken(email, since) {
  const deadline = Date.now() + TOKEN_WAIT_MS;
  while (Date.now() < deadline) {
    const entry = inbox.get(email.toLowerCase());
    if (entry && entry.at >= since) return entry.token;
    await sleep(300);
  }
  return null;
}

// ───────────────────────── Từng tài khoản ─────────────────────────

async function confirmViaMailbox(email, since) {
  const token = await waitForToken(email, since);
  if (!token) return 'Không nhận được email xác thực (backend đã trỏ Email__ResendApiUrl vào hộp thư giả chưa?)';
  const res = await request('POST', '/auth/confirm-email', { body: { token } });
  return res.status === 200 ? null : `confirm-email HTTP ${res.status} ${res.text}`;
}

// Trả null nếu tài khoản sẵn sàng, hoặc chuỗi mô tả lỗi.
async function provision(i) {
  const email = emailOf(i);
  const since = Date.now();

  const reg = await request('POST', '/auth/register', {
    body: {
      fullName: `Loadtest User ${String(i).padStart(pad, '0')}`,
      email,
      password: PASSWORD,
    },
  });

  if (reg.status === 201) {
    return CONFIRM_MODE === 'mock' ? confirmViaMailbox(email, since) : null;
  }

  // Đã tồn tại (chạy lại script): ValidationAppException 400 kèm errors.email.
  if (reg.status === 400 && reg.body?.errors?.email) {
    // Đăng nhập để biết tài khoản đã xác thực chưa: 200 = xong, 403 = còn chờ xác thực.
    const login = await request('POST', '/auth/login', { body: { email, password: PASSWORD } });
    if (login.status === 200) return null;
    if (login.status === 401) return 'Tài khoản đã tồn tại nhưng mật khẩu khác LOADTEST_PASSWORD';
    if (login.status === 403 && CONFIRM_MODE === 'mock') {
      const resendSince = Date.now();
      await request('POST', '/auth/resend-confirmation', { body: { email } });
      return confirmViaMailbox(email, resendSince);
    }
    if (login.status === 403) return null; // chế độ sql: sẽ được UPDATE ở bước in SQL
    return `login HTTP ${login.status} ${login.text}`;
  }

  return `register HTTP ${reg.status} ${reg.text}`;
}

async function runPool(total, worker) {
  let next = 1;
  const failures = [];
  let done = 0;

  async function lane() {
    while (next <= total) {
      const i = next++;
      let error;
      try {
        error = await worker(i);
      } catch (e) {
        error = e instanceof Error ? e.message : String(e);
      }
      if (error) failures.push({ i, error });
      done++;
      if (done % 25 === 0 || done === total) console.log(`  ${done}/${total}`);
    }
  }

  await Promise.all(Array.from({ length: Math.min(CONCURRENCY, total) }, lane));
  return failures;
}

// ───────────────────────── Chạy ─────────────────────────

console.log(`Server: ${BASE_URL} | số tài khoản: ${COUNT} | xác thực: ${CONFIRM_MODE}`);
console.log(`Email mẫu: ${emailOf(1)} | mật khẩu: ${PASSWORD}`);

let mailbox = null;
if (CONFIRM_MODE === 'mock') {
  try {
    mailbox = await startMailbox();
  } catch (e) {
    fail(`Không mở được hộp thư giả ở cổng ${MOCK_PORT}: ${e.message}. Đặt MOCK_MAIL_PORT khác (và trỏ backend tới cổng đó).`);
  }
  console.log(`Hộp thư giả: http://0.0.0.0:${MOCK_PORT}/emails (backend phải trỏ Email__ResendApiUrl vào đây)`);
}

const failures = await runPool(COUNT, provision);
if (mailbox) await new Promise((resolve) => mailbox.close(resolve));

const failedIndexes = new Set(failures.map((f) => f.i));
const users = [];
for (let i = 1; i <= COUNT; i++) {
  if (!failedIndexes.has(i)) users.push({ email: emailOf(i), password: PASSWORD, portal: 'customer' });
}

if (users.length > 0) {
  writeFileSync(USERS_FILE, `${JSON.stringify(users, null, 2)}\n`, 'utf8');
  console.log(`Đã ghi ${users.length} tài khoản vào ${USERS_FILE}`);
} else {
  console.error('Không có tài khoản nào thành công: giữ nguyên users.json.');
}

if (CONFIRM_MODE === 'sql') {
  console.log(
    '\nChạy câu sau trên DB staging để xác thực email, rồi mới chạy k6:\n\n' +
      `UPDATE "Users" SET "EmailConfirmed" = TRUE WHERE "Email" LIKE 'loadtest.user%@${EMAIL_DOMAIN}';\n`,
  );
}

if (failures.length > 0) {
  console.error(`\n${failures.length} tài khoản thất bại (hiển thị tối đa 10):`);
  for (const f of failures.slice(0, 10)) console.error(`  #${f.i} ${emailOf(f.i)}: ${f.error}`);
  process.exit(1);
}
