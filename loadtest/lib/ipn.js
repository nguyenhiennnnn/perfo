// Tạo payload IPN VNPay ký HMAC-SHA512 đúng như backend kiểm tra.
//
// Nguồn đối chiếu (backend/Cinema.Infrastructure/Payments):
//   - VnPayGateway.VerifyCallback: bỏ vnp_SecureHash, vnp_SecureHashType và mọi tham số RỖNG; sắp xếp
//     theo tên (StringComparer.Ordinal); ghép "key=value" bằng "&" với cả key và value qua
//     WebUtility.UrlEncode; HMAC-SHA512 với HashSecret (đã Trim); so khớp hex không phân biệt hoa thường.
//   - HmacSigner.Sha512Hex: HMAC-SHA512 khóa/dữ liệu UTF-8, ra hex chữ thường.
//   - PaymentService.ProcessVnPayIpnAsync: tìm Payment theo vnp_TxnRef, so vnp_Amount / 100 với
//     Payment.Amount (sai số 0,01), vnp_ResponseCode == "00" là thành công.
//   - PaymentsController.VnPayIpn: GET/POST /api/v1/payments/vnpay/ipn, đọc tham số từ query + form,
//     trả JSON thô { rspCode, message } (RawResponse, không bọc { success, data }).
//
// Biến môi trường (k6 run -e ...):
//   VNPAY_HASH_SECRET  BẮT BUỘC. Phải trùng VnPay:HashSecret của backend staging.
//   VNPAY_TMN_CODE     tùy chọn; backend không kiểm tra TmnCode khi verify, nên chỉ để payload giống thật.

import crypto from 'k6/crypto';
import { API_URL } from './config.js';

export const VNPAY_HASH_SECRET = String(__ENV.VNPAY_HASH_SECRET || '').trim();
export const VNPAY_TMN_CODE = String(__ENV.VNPAY_TMN_CODE || '').trim();

export const IPN_PATH = '/payments/vnpay/ipn';

// Phản hồi backend trả cho IPN hợp lệ: lần đầu xử lý xong VÀ cả lần trùng (IsAlreadyProcessed) đều là cặp này.
export const EXPECTED_RSP_CODE = '00';
export const EXPECTED_RSP_MESSAGE = 'Confirm Success';

const VNPAY_UTC_OFFSET_MS = 7 * 60 * 60 * 1000; // VnPayGateway.FormatVnPayDate: UTC+7

// Mô phỏng WebUtility.UrlEncode của .NET: giữ nguyên A-Z a-z 0-9 và - _ . ! * ( ); khoảng trắng thành "+";
// ký tự khác thành %XX (UTF-8, hex HOA). encodeURIComponent khác ở ba điểm: giữ ~ và ', và mã hóa khoảng trắng thành %20.
export function dotnetUrlEncode(value) {
  return encodeURIComponent(value)
    .replace(/~/g, '%7E')
    .replace(/'/g, '%27')
    .replace(/%20/g, '+');
}

function isSigned(key, value) {
  if (key === 'vnp_SecureHash' || key === 'vnp_SecureHashType') return false;
  return value !== undefined && value !== null && String(value) !== '';
}

// Chuỗi dữ liệu để ký, đúng BuildSignData của backend. Cũng chính là query string gửi đi
// (backend giải mã query rồi dựng lại cùng chuỗi này).
export function buildSignData(params) {
  return Object.keys(params)
    .filter((key) => isSigned(key, params[key]))
    .sort() // so sánh theo mã UTF-16, tương đương StringComparer.Ordinal
    .map((key) => `${dotnetUrlEncode(key)}=${dotnetUrlEncode(String(params[key]))}`)
    .join('&');
}

function requireSecret(secret) {
  const value = String(secret === undefined ? VNPAY_HASH_SECRET : secret).trim();
  if (value === '') {
    throw new Error('Thiếu VNPAY_HASH_SECRET. Chạy: k6 run -e VNPAY_HASH_SECRET=<VnPay:HashSecret của backend> ...');
  }
  return value;
}

// HMAC-SHA512 hex chữ thường của chuỗi ký.
export function signIpnParams(params, secret) {
  return crypto.hmac('sha512', requireSecret(secret), buildSignData(params), 'hex');
}

// "yyyyMMddHHmmss" theo giờ Việt Nam, giống FormatVnPayDate.
export function formatVnPayDate(date) {
  const ms = (date instanceof Date ? date : new Date(date)).getTime();
  return new Date(ms + VNPAY_UTC_OFFSET_MS).toISOString().replace(/[-:T]/g, '').slice(0, 14);
}

// Lấy vnp_TxnRef và vnp_Amount (đã nhân 100) từ paymentUrl mà POST /bookings/{id}/checkout trả về.
export function parsePaymentUrl(paymentUrl) {
  const query = String(paymentUrl).split('?')[1] || '';
  const out = {};
  for (const pair of query.split('&')) {
    const eq = pair.indexOf('=');
    if (eq === -1) continue;
    const key = decodeURIComponent(pair.slice(0, eq).replace(/\+/g, ' '));
    out[key] = decodeURIComponent(pair.slice(eq + 1).replace(/\+/g, ' '));
  }
  if (!out.vnp_TxnRef || !out.vnp_Amount) {
    throw new Error(`paymentUrl thiếu vnp_TxnRef/vnp_Amount: ${paymentUrl}`);
  }
  return { txnRef: out.vnp_TxnRef, amountVnp: out.vnp_Amount };
}

// Tham số IPN (chưa ký). Mọi giá trị là hàm của đầu vào: gọi lại với cùng đầu vào cho đúng payload cũ,
// nên IPN "trùng" là bản gửi lại y hệt (cùng vnp_TransactionNo, vnp_PayDate, chữ ký) như VNPay thật.
//   txnRef        vnp_TxnRef = Payment.TransactionId
//   amountVnp     vnp_Amount = số tiền x 100, dạng chuỗi (lấy nguyên từ paymentUrl)
//   transactionNo vnp_TransactionNo, mã giao dịch phía VNPay (phải ổn định giữa các lần gửi lại)
//   payDate       vnp_PayDate, "yyyyMMddHHmmss" (xem formatVnPayDate)
//   responseCode  mặc định "00" (thành công)
export function buildIpnParams({ txnRef, amountVnp, transactionNo, payDate, responseCode }) {
  const code = responseCode === undefined ? '00' : String(responseCode);
  return {
    vnp_TmnCode: VNPAY_TMN_CODE,
    vnp_Amount: String(amountVnp),
    vnp_BankCode: 'NCB',
    vnp_BankTranNo: `VNP${transactionNo}`,
    vnp_CardType: 'ATM',
    vnp_OrderInfo: `Loadtest IPN ${txnRef}`,
    vnp_PayDate: payDate,
    vnp_ResponseCode: code,
    vnp_TransactionNo: String(transactionNo),
    vnp_TransactionStatus: code === '00' ? '00' : '02',
    vnp_TxnRef: txnRef,
  };
}

// Query string đã ký: "<các tham số đã mã hóa>&vnp_SecureHash=<hex>".
export function buildIpnQuery(params, secret) {
  return `${buildSignData(params)}&vnp_SecureHash=${signIpnParams(params, secret)}`;
}

// URL đầy đủ để http.get (VNPay gọi IPN bằng GET).
export function ipnUrl(params, secret) {
  return `${API_URL}${IPN_PATH}?${buildIpnQuery(params, secret)}`;
}

// Đọc { rspCode, message } từ response IPN; null nếu body không phải JSON.
export function parseIpnResponse(res) {
  let body;
  try {
    body = res.json();
  } catch (e) {
    return null;
  }
  if (body === null || typeof body !== 'object') return null;
  const code = body.rspCode !== undefined ? body.rspCode : body.RspCode;
  const message = body.message !== undefined ? body.message : body.Message;
  return { rspCode: code === undefined ? undefined : String(code), message };
}

// true nếu HTTP 200 và đúng { "00", "Confirm Success" }.
export function isExpectedIpnResponse(res) {
  if (res.status !== 200) return false;
  const rsp = parseIpnResponse(res);
  return rsp !== null && rsp.rspCode === EXPECTED_RSP_CODE && rsp.message === EXPECTED_RSP_MESSAGE;
}
