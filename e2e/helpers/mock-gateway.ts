import { createServer, type IncomingMessage, type Server } from "node:http";

/**
 * Máy chủ giả lập (chạy ngay trong tiến trình test) thay cho hai dịch vụ ngoài mà backend gọi khi hoàn tiền:
 *  - POST /transaction: API hoàn tiền của VNPay (VnPay:ApiUrl). Luôn trả thành công và ghi lại yêu cầu.
 *  - POST /emails: API gửi mail của Resend (Email:ResendApiUrl). Ghi lại thư thay cho hộp thư thật (outbox).
 *
 * Backend CHỈ gọi vào đây khi được cấu hình trỏ tới (xem .env.e2e.example), ví dụ:
 *   VnPay__ApiUrl=http://localhost:9099/transaction
 *   Email__ResendApiKey=e2e-fake-key
 *   Email__ResendApiUrl=http://localhost:9099/emails
 */

export const MOCK_GATEWAY_PORT = Number(process.env.E2E_MOCK_GATEWAY_PORT) || 9099;

const VNPAY_AMOUNT_SCALE = 100;
const HTTP_OK = 200;
const HTTP_NOT_FOUND = 404;
/** vnp_ResponseCode "00" + vnp_TransactionStatus khác "09" = VNPay chấp nhận hoàn tiền (VnPayGateway.RefundAsync). */
const VNPAY_ACCEPTED_CODE = "00";
const VNPAY_REFUND_DONE_STATUS = "05";

export interface MockRefundCall {
  /** vnp_TxnRef = Payment.TransactionId của giao dịch gốc. */
  txnRef: string;
  /** Số tiền hoàn (VND, đã chia 100 từ vnp_Amount). */
  amount: number;
  /** "02" hoàn toàn phần, "03" hoàn một phần. */
  transactionType: string;
  receivedAt: number;
}

export interface MockEmail {
  to: string;
  subject: string;
  html: string;
  receivedAt: number;
}

export interface MockGateway {
  readonly refunds: readonly MockRefundCall[];
  readonly emails: readonly MockEmail[];
  findRefund: (txnRef: string) => MockRefundCall | undefined;
  /** Thư đầu tiên gửi tới `to` có tiêu đề khớp `subject` và nội dung nhắc tới `bookingCode`. */
  findEmail: (query: {
    to: string;
    subject: RegExp;
    bookingCode: string;
  }) => MockEmail | undefined;
  close: () => Promise<void>;
}

async function readJson(request: IncomingMessage): Promise<Record<string, unknown>> {
  const chunks: Buffer[] = [];
  for await (const chunk of request) chunks.push(chunk as Buffer);
  try {
    return JSON.parse(Buffer.concat(chunks).toString("utf8")) as Record<string, unknown>;
  } catch {
    return {};
  }
}

function listen(server: Server, port: number): Promise<void> {
  return new Promise((resolve, reject) => {
    server.once("error", (error: NodeJS.ErrnoException) => {
      reject(
        error.code === "EADDRINUSE"
          ? new Error(
              `Cổng ${port} của mock gateway đang bị chiếm. Tắt tiến trình đang dùng hoặc đặt E2E_MOCK_GATEWAY_PORT khác (và trỏ backend tới cổng đó).`,
            )
          : error,
      );
    });
    server.listen(port, resolve);
  });
}

export async function startMockGateway(port = MOCK_GATEWAY_PORT): Promise<MockGateway> {
  const refunds: MockRefundCall[] = [];
  const emails: MockEmail[] = [];

  const server = createServer((request, response) => {
    const reply = (status: number, body: unknown) => {
      response.writeHead(status, { "Content-Type": "application/json" });
      response.end(JSON.stringify(body));
    };

    void (async () => {
      const path = (request.url ?? "").split("?")[0];
      if (request.method !== "POST") return reply(HTTP_NOT_FOUND, {});

      if (path === "/transaction") {
        const body = await readJson(request);
        refunds.push({
          txnRef: String(body.vnp_TxnRef ?? ""),
          amount: Number(body.vnp_Amount) / VNPAY_AMOUNT_SCALE,
          transactionType: String(body.vnp_TransactionType ?? ""),
          receivedAt: Date.now(),
        });
        return reply(HTTP_OK, {
          vnp_ResponseCode: VNPAY_ACCEPTED_CODE,
          vnp_Message: "Refund success (mock E2E)",
          vnp_TransactionNo: `MOCK${Date.now()}`,
          vnp_TransactionStatus: VNPAY_REFUND_DONE_STATUS,
        });
      }

      if (path === "/emails") {
        const body = await readJson(request);
        const recipients = Array.isArray(body.to) ? body.to : [body.to];
        for (const to of recipients) {
          emails.push({
            to: String(to ?? ""),
            subject: String(body.subject ?? ""),
            html: String(body.html ?? ""),
            receivedAt: Date.now(),
          });
        }
        return reply(HTTP_OK, { id: `mock-${Date.now()}` });
      }

      return reply(HTTP_NOT_FOUND, {});
    })();
  });

  await listen(server, port);

  return {
    refunds,
    emails,
    findRefund: (txnRef) => refunds.find((refund) => refund.txnRef === txnRef),
    findEmail: ({ to, subject, bookingCode }) =>
      emails.find(
        (email) =>
          email.to.toLowerCase() === to.toLowerCase() &&
          subject.test(email.subject) &&
          (email.subject.includes(bookingCode) || email.html.includes(bookingCode)),
      ),
    close: () =>
      new Promise<void>((resolve) => {
        server.closeAllConnections();
        server.close(() => resolve());
      }),
  };
}
