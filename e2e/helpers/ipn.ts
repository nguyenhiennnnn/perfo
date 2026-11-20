import { createHmac } from "node:crypto";
import type { APIRequestContext } from "@playwright/test";
import { API_BASE_URL } from "./auth";

/**
 * Giả lập IPN của VNPay gọi vào backend (GET/POST /api/v1/payments/vnpay/ipn).
 * Thuật toán ký khớp VnPayGateway.VerifyCallback (backend/Cinema.Infrastructure/Payments):
 *  - bỏ vnp_SecureHash, vnp_SecureHashType và mọi tham số có giá trị rỗng;
 *  - sắp xếp theo tên tham số (so sánh ordinal);
 *  - nối "key=value" bằng "&", mã hóa key/value kiểu WebUtility.UrlEncode của .NET;
 *  - HMAC-SHA512 với VnPay:HashSecret (đã Trim), xuất hex chữ thường.
 */

const SECURE_HASH_KEY = "vnp_SecureHash";
const SECURE_HASH_TYPE_KEY = "vnp_SecureHashType";
const AMOUNT_SCALE = 100;
const VNPAY_UTC_OFFSET_MS = 7 * 60 * 60 * 1000;

export type IpnPayload = Record<string, string>;

export interface IpnInput {
  /** vnp_TxnRef: bằng Payment.TransactionId (backend sinh khi tạo thanh toán). */
  txnRef: string;
  /** Số tiền VND (chưa nhân 100). Backend so khớp với Payment.Amount. */
  amount: number;
  /** vnp_ResponseCode: "00" = thành công, mã khác = thất bại. Mặc định "00". */
  responseCode?: string;
  /** vnp_TransactionNo: mã giao dịch phía VNPay. Mặc định sinh ngẫu nhiên. */
  transactionNo?: string;
  tmnCode?: string;
  bankCode?: string;
  orderInfo?: string;
  /** Tham số bổ sung/ghi đè; giá trị rỗng sẽ bị bỏ khỏi payload. */
  extra?: Record<string, string>;
}

export interface IpnResponse {
  status: number;
  /** "00" xử lý/xác nhận thành công, "97" sai chữ ký, "01" không thấy đơn, "04" sai số tiền, "99" chưa cấu hình. */
  rspCode: string;
  message: string;
}

/** Bản sao WebUtility.UrlEncode của .NET: giữ A-Z a-z 0-9 - _ . ! * ( ), khoảng trắng -> "+", còn lại %XX (hoa) trên byte UTF-8. */
export function dotnetUrlEncode(value: string): string {
  let out = "";
  for (const byte of Buffer.from(value, "utf8")) {
    const isAlnum =
      (byte >= 0x30 && byte <= 0x39) ||
      (byte >= 0x41 && byte <= 0x5a) ||
      (byte >= 0x61 && byte <= 0x7a);
    if (isAlnum || "-_.!*()".includes(String.fromCharCode(byte))) {
      out += String.fromCharCode(byte);
    } else if (byte === 0x20) {
      out += "+";
    } else {
      out += `%${byte.toString(16).toUpperCase().padStart(2, "0")}`;
    }
  }
  return out;
}

/** Chuỗi dữ liệu đem ký (không gồm vnp_SecureHash/vnp_SecureHashType và giá trị rỗng). */
export function buildVnPaySignData(params: Record<string, string>): string {
  return Object.entries(params)
    .filter(
      ([key, value]) =>
        key !== SECURE_HASH_KEY &&
        key !== SECURE_HASH_TYPE_KEY &&
        value !== "",
    )
    .sort(([a], [b]) => (a < b ? -1 : a > b ? 1 : 0))
    .map(([key, value]) => `${dotnetUrlEncode(key)}=${dotnetUrlEncode(value)}`)
    .join("&");
}

/** Secret phải trùng VnPay:HashSecret (VNPAY_HASH_SECRET) của backend đang chạy E2E. */
function resolveHashSecret(hashSecret?: string): string {
  const secret = (hashSecret ?? process.env.E2E_VNPAY_HASH_SECRET ?? "").trim();
  if (!secret) {
    throw new Error(
      "Thiếu hash secret VNPay: đặt E2E_VNPAY_HASH_SECRET (trùng VNPAY_HASH_SECRET của backend) hoặc truyền tham số hashSecret.",
    );
  }
  return secret;
}

/** HMAC-SHA512 hex chữ thường của chuỗi ký. */
export function vnPayHmac(secret: string, signData: string): string {
  return createHmac("sha512", secret.trim()).update(signData, "utf8").digest("hex");
}

function formatVnPayDate(date: Date): string {
  const local = new Date(date.getTime() + VNPAY_UTC_OFFSET_MS);
  const pad = (n: number) => String(n).padStart(2, "0");
  return (
    `${local.getUTCFullYear()}${pad(local.getUTCMonth() + 1)}${pad(local.getUTCDate())}` +
    `${pad(local.getUTCHours())}${pad(local.getUTCMinutes())}${pad(local.getUTCSeconds())}`
  );
}

/** Tạo payload IPN có vnp_SecureHash đúng như backend kiểm tra. */
export function signIpn(input: IpnInput, hashSecret?: string): IpnPayload {
  const secret = resolveHashSecret(hashSecret);
  const responseCode = input.responseCode ?? "00";
  const success = responseCode === "00";

  const unsigned: IpnPayload = {
    vnp_TmnCode: input.tmnCode ?? process.env.E2E_VNPAY_TMN_CODE ?? "E2ETMN01",
    vnp_Amount: String(Math.round(input.amount * AMOUNT_SCALE)),
    vnp_BankCode: input.bankCode ?? "NCB",
    vnp_BankTranNo: `E2E${Date.now()}`,
    vnp_CardType: "ATM",
    vnp_OrderInfo: input.orderInfo ?? "Thanh toan don hang E2E",
    vnp_PayDate: formatVnPayDate(new Date()),
    vnp_ResponseCode: responseCode,
    vnp_TransactionNo:
      input.transactionNo ?? String(Math.floor(10_000_000 + Math.random() * 89_999_999)),
    vnp_TransactionStatus: success ? "00" : "02",
    vnp_TxnRef: input.txnRef,
    ...input.extra,
  };

  const payload: IpnPayload = {};
  for (const [key, value] of Object.entries(unsigned)) {
    if (value !== "") payload[key] = value;
  }

  payload[SECURE_HASH_KEY] = vnPayHmac(secret, buildVnPaySignData(payload));
  return payload;
}

/**
 * Gọi IPN của backend bằng GET (đúng cách VNPay gọi). Truyền payload từ signIpn(); muốn test
 * chữ ký sai hoặc số tiền sai thì sửa payload sau khi ký. Không ném lỗi theo HTTP status.
 */
export async function sendIpn(
  request: APIRequestContext,
  payload: IpnPayload,
): Promise<IpnResponse> {
  const response = await request.get(`${API_BASE_URL}/payments/vnpay/ipn`, {
    params: payload,
    failOnStatusCode: false,
  });

  const text = await response.text();
  let body: Record<string, unknown> = {};
  try {
    body = JSON.parse(text) as Record<string, unknown>;
  } catch {
    // Phản hồi không phải JSON: giữ body rỗng, rspCode sẽ là chuỗi rỗng để test tự phát hiện.
  }

  return {
    status: response.status(),
    rspCode: String(body.rspCode ?? body.RspCode ?? ""),
    message: String(body.message ?? body.Message ?? text),
  };
}
