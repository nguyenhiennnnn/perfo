import { randomUUID } from "node:crypto";
import { expect, type APIRequestContext } from "@playwright/test";
import { callApi } from "./api-call";
import { startMockGateway, type MockGateway } from "./mock-gateway";

/**
 * Hỗ trợ cho test đăng nhập, reset mật khẩu và buộc đổi mật khẩu:
 *
 * 1) LẤY TOKEN QUA OUTBOX EMAIL GIẢ LẬP. Backend không bao giờ trả token reset/xác thực qua API, chỉ gửi trong
 *    email. Test dùng lại mock gateway của booking-cancel-refund (./mock-gateway): backend được trỏ
 *    Email__ResendApiUrl tới mock nên mọi email đi ra bị bắt vào "outbox" trong bộ nhớ; test đọc thư của đúng
 *    người nhận rồi tách token khỏi link `.../auth/reset-password?token=...` trong nội dung HTML. Đây là đúng
 *    thứ người dùng nhận được, không cần sửa backend hay đọc DB.
 * 2) TÀI KHOẢN RIÊNG CHO MỖI LẦN CHẠY (email ngẫu nhiên) để đổi mật khẩu không làm hỏng tài khoản seed mà
 *    các spec khác dùng: khách tự đăng ký + xác thực email qua outbox, nhân viên do admin tạo với mật khẩu tạm.
 */

const OUTBOX_ACQUIRE_WAIT_MS = 300_000;
const OUTBOX_ACQUIRE_INTERVAL_MS = 2_000;
const EMAIL_WAIT_MS = 90_000;
const EMAIL_POLL_INTERVALS_MS = [500, 1_000, 2_000];
const HTTP_CREATED = 201;

/** Mật khẩu mà các test dùng khi tạo tài khoản (đạt StrongPassword: hoa, thường, số, tối thiểu 8 ký tự). */
export const E2E_INITIAL_PASSWORD = "Customer@123";

export const OUTBOX_HINT =
  "Backend phải chạy với Email__ResendApiKey và Email__ResendApiUrl trỏ tới mock gateway (xem .env.e2e.example).";

/** Tiêu đề thư (Messages.resx) và đường dẫn link của từng loại thư bảo mật. */
export const EMAIL_KINDS = {
  confirmEmail: { subject: /^Xác thực email/, linkPath: "auth/confirm-email" },
  passwordReset: { subject: /^Đặt lại mật khẩu/, linkPath: "auth/reset-password" },
} as const;

export type EmailKind = keyof typeof EMAIL_KINDS;

/**
 * Bật outbox giả lập. Backend chỉ trỏ được tới MỘT mock (cổng 9099) nên spec khác (booking-cancel-refund) cũng dùng
 * cổng này: nếu đang bị chiếm thì chờ spec kia tắt rồi mới bật, thay vì thất bại khi chạy song song nhiều worker.
 */
export async function acquireOutbox(): Promise<MockGateway> {
  const deadline = Date.now() + OUTBOX_ACQUIRE_WAIT_MS;
  for (;;) {
    try {
      return await startMockGateway();
    } catch (error) {
      const portBusy = error instanceof Error && /đang bị chiếm/.test(error.message);
      if (!portBusy || Date.now() >= deadline) throw error;
      await new Promise((resolve) => setTimeout(resolve, OUTBOX_ACQUIRE_INTERVAL_MS));
    }
  }
}

/**
 * Chờ thư mới nhất gửi tới `to` (nhận sau thời điểm `since`) rồi trả về token trong link.
 * `since` = Date.now() lấy TRƯỚC khi kích hoạt việc gửi thư, để không nhầm với thư cũ.
 */
export async function waitForEmailToken(
  mock: MockGateway,
  kind: EmailKind,
  to: string,
  since: number,
): Promise<string> {
  const { subject, linkPath } = EMAIL_KINDS[kind];
  const linkPattern = new RegExp(`${linkPath}\\?token=([A-Za-z0-9_-]+)`);
  let token = "";

  await expect
    .poll(
      () => {
        const mail = [...mock.emails]
          .reverse()
          .find(
            (email) =>
              email.receivedAt >= since &&
              email.to.toLowerCase() === to.toLowerCase() &&
              subject.test(email.subject),
          );
        token = (mail && linkPattern.exec(mail.html)?.[1]) || "";
        return token;
      },
      {
        message: `Không thấy thư "${kind}" gửi tới ${to} trong outbox. ${OUTBOX_HINT}`,
        timeout: EMAIL_WAIT_MS,
        intervals: EMAIL_POLL_INTERVALS_MS,
      },
    )
    .not.toBe("");

  return token;
}

/** Email ngẫu nhiên, không trùng giữa các lần chạy. */
export function uniqueE2eEmail(prefix: string): string {
  return `${prefix}.${randomUUID().slice(0, 8)}@cinema.local`;
}

export interface ProvisionedAccount {
  userId: number;
  email: string;
  password: string;
}

/** Khách tự đăng ký (POST /auth/register) rồi xác thực email bằng token lấy từ outbox: tài khoản đăng nhập được. */
export async function registerConfirmedCustomer(
  request: APIRequestContext,
  mock: MockGateway,
  email: string,
  password = E2E_INITIAL_PASSWORD,
): Promise<ProvisionedAccount> {
  const since = Date.now();
  const register = await callApi<{ userId: number }>(request, {
    method: "POST",
    path: "/auth/register",
    data: { fullName: "Khách E2E Đăng nhập", email, password },
  });
  if (register.status !== HTTP_CREATED || !register.json?.data) {
    throw new Error(`Đăng ký ${email} thất bại: HTTP ${register.status} ${register.text}`);
  }

  const token = await waitForEmailToken(mock, "confirmEmail", email, since);
  const confirm = await callApi(request, {
    method: "POST",
    path: "/auth/confirm-email",
    data: { token },
  });
  if (confirm.status !== 200) {
    throw new Error(`Xác thực email ${email} thất bại: HTTP ${confirm.status} ${confirm.text}`);
  }

  return { userId: register.json.data.userId, email, password };
}

export interface ProvisionedEmployee extends ProvisionedAccount {
  employeeId: number;
}

/** Admin tạo nhân viên với mật khẩu tạm (POST /admin/employees/direct): User.MustChangePassword = true. */
export async function createEmployeeWithTemporaryPassword(
  request: APIRequestContext,
  adminToken: string,
  email: string,
  temporaryPassword: string,
): Promise<ProvisionedEmployee> {
  const created = await callApi<{ employeeId: number; userId: number }>(request, {
    method: "POST",
    path: "/admin/employees/direct",
    token: adminToken,
    data: { fullName: "Nhân viên E2E Đổi mật khẩu", email, temporaryPassword },
  });
  if (created.status !== HTTP_CREATED || !created.json?.data) {
    throw new Error(`Tạo nhân viên ${email} thất bại: HTTP ${created.status} ${created.text}`);
  }

  const { employeeId, userId } = created.json.data;
  return { employeeId, userId, email, password: temporaryPassword };
}

/** Dọn sau test (không ném lỗi): khóa tài khoản khách / ngừng hoạt động nhân viên vì không có API xóa. */
export async function deactivateAccounts(
  request: APIRequestContext,
  adminToken: string,
  accounts: { customers: ProvisionedAccount[]; employees: ProvisionedEmployee[] },
): Promise<void> {
  for (const { employeeId } of accounts.employees) {
    await callApi(request, {
      method: "PUT",
      path: `/admin/employees/${employeeId}`,
      token: adminToken,
      data: { positionId: null, hireDate: null, isActive: false },
    }).catch(() => undefined);
  }
  for (const { userId } of accounts.customers) {
    await callApi(request, {
      method: "PUT",
      path: `/admin/users/${userId}/status`,
      token: adminToken,
      data: { isActive: false },
    }).catch(() => undefined);
  }
}

/** Đăng nhập bằng API với tài khoản bất kỳ (không qua ACCOUNTS); trả về nguyên phản hồi để test khẳng định. */
export function loginRequest(
  request: APIRequestContext,
  portal: "customer" | "admin",
  email: string,
  password: string,
) {
  return callApi<{
    accessToken: string;
    user: { mustChangePassword?: boolean; role: string };
  }>(request, {
    method: "POST",
    path: portal === "admin" ? "/auth/admin-login" : "/auth/login",
    data: { email, password },
  });
}
