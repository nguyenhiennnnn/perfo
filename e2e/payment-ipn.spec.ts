import type { APIRequestContext, PlaywrightWorkerArgs } from "@playwright/test";
import { expect, test } from "./fixtures";
import { getAccessToken } from "./helpers/auth";
import {
  BOOKING_STATUS,
  cancelBookingViaApi,
  checkoutViaApi,
  getBookingDetail,
  holdSeatsViaApi,
  PAYMENT_STATUS,
  type CheckoutResult,
  type HoldResult,
} from "./helpers/booking-api";
import {
  getSeatStatus,
  getShowtimeSeats,
  pickAdjacentSeats,
  SEAT_STATUS,
  setHoldDurationMinutes,
  type ShowtimeSeat,
} from "./helpers/booking-flow";
import { sendIpn, signIpn, type IpnPayload } from "./helpers/ipn";
import {
  cancelShowtime,
  createShowtimeIn,
  SHOWTIME_OFFSETS_MINUTES,
  type CreatedShowtime,
} from "./helpers/showtimes";

/** Mã phản hồi của endpoint IPN: "00" xác nhận đã nhận, "97" sai chữ ký. */
const IPN_CONFIRMED_CODE = "00";
const IPN_INVALID_SIGNATURE_CODE = "97";
/** vnp_ResponseCode "24": khách hủy giao dịch tại cổng VNPay (mã thất bại bất kỳ khác "00" đều được). */
const VNPAY_CANCELLED_CODE = "24";
const WRONG_HASH_SECRET = "secret-sai-cua-e2e";

const SETUP_TIMEOUT_MS = 120_000;
const API_TEST_TIMEOUT_MS = 60_000;
/** Giữ ghế 1 phút + job ReleaseExpiredSeats chạy mỗi phút + thời gian IPN trễ chờ hoàn tiền tự động. */
const LATE_IPN_TEST_TIMEOUT_MS = 360_000;
const SHORT_HOLD_MINUTES = 1;
const SERVER_EXPIRY_WAIT_MS = 150_000;
const SEAT_STATE_WAIT_MS = 30_000;
const POLL_INTERVAL_MS = 3_000;

const SEATS_FOR_LATE_IPN = 1;
const SEATS_FOR_IPN_CASES = 3;

type Playwright = PlaywrightWorkerArgs["playwright"];

interface PreparedShowtime {
  showtime: CreatedShowtime;
  seats: ShowtimeSeat[];
}

interface Tokens {
  admin: string;
  customer: string;
  customer2: string;
}

/** Chọn mỗi ghế ở một hàng khác nhau để các đơn trong cùng suất không dính luật "ghế lẻ" của nhau. */
function pickSeatsInDistinctRows(seats: ShowtimeSeat[], count: number): ShowtimeSeat[] {
  const picked: ShowtimeSeat[] = [];
  for (let i = 0; i < count; i++) {
    const usedRows = new Set(picked.map((seat) => seat.seatRow));
    const [seat] = pickAdjacentSeats(
      seats.filter((candidate) => !usedRows.has(candidate.seatRow)),
      1,
    );
    picked.push(seat);
  }
  return picked;
}

/** Tạo suất +3 giờ ở phòng không nằm trong excludeRoomIds (tránh tự hủy suất của nhóm test kia). */
async function prepareShowtime(
  playwright: Playwright,
  adminToken: string,
  seatCount: number,
  excludeRoomIds: number[],
): Promise<PreparedShowtime> {
  const api = await playwright.request.newContext();
  try {
    const showtime = await createShowtimeIn(api, adminToken, SHOWTIME_OFFSETS_MINUTES.in3h, {
      excludeRoomIds,
    });
    const seats = pickSeatsInDistinctRows(
      await getShowtimeSeats(api, showtime.showtimeId),
      seatCount,
    );
    return { showtime, seats };
  } finally {
    await api.dispose();
  }
}

let tokens: Tokens;
let ipnCases: PreparedShowtime;
let lateIpn: PreparedShowtime;

test.beforeAll(async ({ playwright }) => {
  test.setTimeout(SETUP_TIMEOUT_MS);
  const api = await playwright.request.newContext();
  try {
    // Đăng nhập một lần cho cả file: endpoint auth bị rate limit 5 lần / 60 giây theo IP.
    tokens = {
      admin: await getAccessToken(api, "admin"),
      customer: await getAccessToken(api, "customer"),
      customer2: await getAccessToken(api, "customer2"),
    };
  } finally {
    await api.dispose();
  }

  ipnCases = await prepareShowtime(playwright, tokens.admin, SEATS_FOR_IPN_CASES, []);
  lateIpn = await prepareShowtime(playwright, tokens.admin, SEATS_FOR_LATE_IPN, [
    ipnCases.showtime.roomId,
  ]);
});

test.afterAll(async ({ playwright }) => {
  if (!tokens) return;
  const api = await playwright.request.newContext();
  try {
    for (const prepared of [ipnCases, lateIpn]) {
      if (!prepared) continue;
      try {
        await cancelShowtime(api, tokens.admin, prepared.showtime.showtimeId);
      } catch {
        // Suất đã có đơn xác nhận/ghế đang giữ nên backend có thể từ chối hủy; DB E2E riêng nên bỏ qua.
      }
    }
  } finally {
    await api.dispose();
  }
});

/** Giữ ghế rồi tạo giao dịch VNPay: trả về đơn và thông tin để ký IPN. */
async function holdAndCheckout(
  request: APIRequestContext,
  accessToken: string,
  prepared: PreparedShowtime,
  seat: ShowtimeSeat,
): Promise<{ hold: HoldResult; payment: CheckoutResult }> {
  const hold = await holdSeatsViaApi(request, accessToken, {
    showtimeId: prepared.showtime.showtimeId,
    seatIds: [seat.showtimeSeatId],
  });
  const payment = await checkoutViaApi(request, accessToken, hold.bookingId);
  return { hold, payment };
}

const seatStatusOf = (
  request: APIRequestContext,
  prepared: PreparedShowtime,
  seat: ShowtimeSeat,
) => getSeatStatus(request, prepared.showtime.showtimeId, seat.showtimeSeatId);

/** Thay ký tự cuối của chuỗi hex để chữ ký chắc chắn khác đi. */
function corruptHash(hash: string): string {
  return `${hash.slice(0, -1)}${hash.endsWith("0") ? "1" : "0"}`;
}

test.describe("Thanh toán thất bại và IPN bất thường (D2, D3, D4)", () => {
  test("D2: IPN thanh toán thất bại thì giao dịch Failed, đơn còn Pending, ghế vẫn được giữ và thanh toán lại được", async ({
    request,
  }) => {
    test.setTimeout(API_TEST_TIMEOUT_MS);
    const [seat] = ipnCases.seats;
    const { customer } = tokens;
    const { hold, payment } = await holdAndCheckout(request, customer, ipnCases, seat);

    try {
      await test.step("Trước IPN: đơn Pending, giao dịch Pending, ghế đang giữ", async () => {
        const detail = await getBookingDetail(request, customer, hold.bookingId);
        expect(detail.status).toBe(BOOKING_STATUS.pending);
        expect(detail.payments).toHaveLength(1);
        expect(detail.payments[0].status).toBe(PAYMENT_STATUS.pending);
        expect(await seatStatusOf(request, ipnCases, seat)).toBe(SEAT_STATUS.held);
      });

      await test.step("VNPay gọi IPN thất bại: backend vẫn xác nhận đã nhận (00)", async () => {
        const response = await sendIpn(
          request,
          signIpn({
            txnRef: payment.txnRef,
            amount: payment.amount,
            responseCode: VNPAY_CANCELLED_CODE,
          }),
        );
        expect(response.status).toBe(200);
        expect(response.rspCode).toBe(IPN_CONFIRMED_CODE);
      });

      await test.step("Sau IPN: giao dịch Failed, đơn còn Pending và chưa có vé, ghế vẫn Held", async () => {
        const detail = await getBookingDetail(request, customer, hold.bookingId);
        expect(detail.status).toBe(BOOKING_STATUS.pending);
        expect(detail.holdExpiresAt).toBeTruthy();
        expect(detail.tickets).toHaveLength(0);
        expect(detail.payments).toHaveLength(1);
        expect(detail.payments[0]).toMatchObject({
          paymentId: payment.paymentId,
          status: PAYMENT_STATUS.failed,
        });
        expect(detail.payments[0].paidAt ?? null).toBeNull();
        expect(await seatStatusOf(request, ipnCases, seat)).toBe(SEAT_STATUS.held);
      });

      await test.step("Thanh toán lại: có giao dịch mới Pending, giao dịch cũ vẫn Failed", async () => {
        const retry = await checkoutViaApi(request, customer, hold.bookingId);
        expect(retry.paymentId).not.toBe(payment.paymentId);
        expect(retry.txnRef).not.toBe(payment.txnRef);

        const detail = await getBookingDetail(request, customer, hold.bookingId);
        expect(detail.status).toBe(BOOKING_STATUS.pending);
        expect(detail.payments).toHaveLength(2);
        const byId = new Map(detail.payments.map((p) => [p.paymentId, p.status]));
        expect(byId.get(retry.paymentId)).toBe(PAYMENT_STATUS.pending);
        expect(byId.get(payment.paymentId)).toBe(PAYMENT_STATUS.failed);
        expect(await seatStatusOf(request, ipnCases, seat)).toBe(SEAT_STATUS.held);
      });
    } finally {
      // Dọn: nhả ghế để khách này giữ được ghế khác trong cùng suất ở test sau.
      await cancelBookingViaApi(request, customer, hold.bookingId);
    }
  });

  test("D3: IPN sai chữ ký bị từ chối (97) và không làm đổi đơn, thanh toán, ghế", async ({
    request,
  }) => {
    test.setTimeout(API_TEST_TIMEOUT_MS);
    const seat = ipnCases.seats[1];
    const { customer } = tokens;
    const { hold, payment } = await holdAndCheckout(request, customer, ipnCases, seat);
    const { txnRef, amount } = payment;

    const forgeries: { name: string; build: () => IpnPayload }[] = [
      {
        name: "chữ ký bị sửa một ký tự",
        build: () => {
          const payload = signIpn({ txnRef, amount });
          payload.vnp_SecureHash = corruptHash(payload.vnp_SecureHash);
          return payload;
        },
      },
      {
        name: "đổi vnp_ResponseCode từ thất bại thành 00 sau khi đã ký",
        build: () => {
          const payload = signIpn({ txnRef, amount, responseCode: VNPAY_CANCELLED_CODE });
          payload.vnp_ResponseCode = IPN_CONFIRMED_CODE;
          return payload;
        },
      },
      {
        name: "đổi vnp_Amount sau khi đã ký",
        build: () => {
          const payload = signIpn({ txnRef, amount });
          payload.vnp_Amount = String(Math.round(Number(payload.vnp_Amount) / 2));
          return payload;
        },
      },
      {
        name: "ký bằng hash secret sai",
        build: () => signIpn({ txnRef, amount }, WRONG_HASH_SECRET),
      },
      {
        name: "thiếu vnp_SecureHash",
        build: () => {
          const payload = signIpn({ txnRef, amount });
          delete payload.vnp_SecureHash;
          return payload;
        },
      },
    ];

    const expectNothingChanged = async () => {
      const detail = await getBookingDetail(request, customer, hold.bookingId);
      expect(detail.status).toBe(BOOKING_STATUS.pending);
      expect(detail.tickets).toHaveLength(0);
      expect(detail.payments).toHaveLength(1);
      expect(detail.payments[0].status).toBe(PAYMENT_STATUS.pending);
      expect(detail.payments[0].paidAt ?? null).toBeNull();
      expect(await seatStatusOf(request, ipnCases, seat)).toBe(SEAT_STATUS.held);
    };

    try {
      await test.step("Trước IPN: đơn Pending, giao dịch Pending, ghế đang giữ", expectNothingChanged);

      for (const forgery of forgeries) {
        await test.step(`IPN giả mạo (${forgery.name}) bị trả mã 97, trạng thái không đổi`, async () => {
          const response = await sendIpn(request, forgery.build());
          expect(response.status).toBe(200);
          expect(response.rspCode).toBe(IPN_INVALID_SIGNATURE_CODE);
          await expectNothingChanged();
        });
      }

      await test.step("IPN hợp lệ sau đó vẫn xác nhận được đơn (các IPN giả không làm hỏng giao dịch)", async () => {
        const response = await sendIpn(request, signIpn({ txnRef, amount }));
        expect(response.rspCode).toBe(IPN_CONFIRMED_CODE);

        const detail = await getBookingDetail(request, customer, hold.bookingId);
        expect(detail.status).toBe(BOOKING_STATUS.confirmed);
        expect(detail.payments[0].status).toBe(PAYMENT_STATUS.success);
        expect(detail.tickets).toHaveLength(1);
        expect(await seatStatusOf(request, ipnCases, seat)).toBe(SEAT_STATUS.booked);
      });
    } finally {
      // Đơn đã Confirmed ở bước cuối thì backend từ chối/hoàn theo luật hủy vé: chỉ dọn khi test dừng giữa chừng.
      const detail = await getBookingDetail(request, customer, hold.bookingId);
      if (detail.status === BOOKING_STATUS.pending) {
        await cancelBookingViaApi(request, customer, hold.bookingId);
      }
    }
  });

  test("D4: IPN thành công gửi trùng chỉ xử lý một lần, IPN thất bại đến sau cũng không đảo ngược đơn", async ({
    request,
  }) => {
    test.setTimeout(API_TEST_TIMEOUT_MS);
    const seat = ipnCases.seats[2];
    const { customer } = tokens;
    const { hold, payment } = await holdAndCheckout(request, customer, ipnCases, seat);
    const successIpn = signIpn({ txnRef: payment.txnRef, amount: payment.amount });

    const expectConfirmedOnce = async (paidAt: string | null | undefined) => {
      const detail = await getBookingDetail(request, customer, hold.bookingId);
      expect(detail.status).toBe(BOOKING_STATUS.confirmed);
      expect(detail.holdExpiresAt ?? null).toBeNull();
      expect(detail.tickets).toHaveLength(1);
      expect(detail.tickets[0]).toMatchObject({ seatRow: seat.seatRow, seatNumber: seat.seatNumber });
      expect(detail.payments).toHaveLength(1);
      expect(detail.payments[0]).toMatchObject({
        paymentId: payment.paymentId,
        status: PAYMENT_STATUS.success,
      });
      expect(detail.payments[0].paidAt).toBeTruthy();
      if (paidAt !== undefined) expect(detail.payments[0].paidAt).toBe(paidAt);
      expect(await seatStatusOf(request, ipnCases, seat)).toBe(SEAT_STATUS.booked);
      return detail.payments[0].paidAt;
    };

    const paidAt = await test.step("Lần IPN thứ nhất: đơn Confirmed, có đúng một vé, ghế Booked", async () => {
      const response = await sendIpn(request, successIpn);
      expect(response.status).toBe(200);
      expect(response.rspCode).toBe(IPN_CONFIRMED_CODE);
      return expectConfirmedOnce(undefined);
    });

    await test.step("Lần IPN thứ hai (cùng payload): vẫn trả 00 và không tạo thêm vé, không đổi thời điểm thanh toán", async () => {
      const response = await sendIpn(request, successIpn);
      expect(response.status).toBe(200);
      expect(response.rspCode).toBe(IPN_CONFIRMED_CODE);
      await expectConfirmedOnce(paidAt);
    });

    await test.step("IPN thất bại đến sau khi đã thành công: trả 00 nhưng đơn và thanh toán giữ nguyên", async () => {
      const response = await sendIpn(
        request,
        signIpn({
          txnRef: payment.txnRef,
          amount: payment.amount,
          responseCode: VNPAY_CANCELLED_CODE,
        }),
      );
      expect(response.status).toBe(200);
      expect(response.rspCode).toBe(IPN_CONFIRMED_CODE);
      await expectConfirmedOnce(paidAt);
    });
  });
});

test.describe("IPN đến trễ sau khi ghế đã bị người khác lấy (C6)", () => {
  let originalHoldMinutes: number | undefined;

  test.beforeAll(async ({ playwright }) => {
    test.setTimeout(SETUP_TIMEOUT_MS);
    const api = await playwright.request.newContext();
    try {
      originalHoldMinutes = await setHoldDurationMinutes(api, tokens.admin, SHORT_HOLD_MINUTES);
    } finally {
      await api.dispose();
    }
  });

  test.afterAll(async ({ playwright }) => {
    if (originalHoldMinutes === undefined) return;
    const api = await playwright.request.newContext();
    try {
      await setHoldDurationMinutes(api, tokens.admin, originalHoldMinutes);
    } catch {
      // Khôi phục best-effort; nếu lỗi, đặt lại thời gian giữ ghế trong Cài đặt rạp bằng tay.
    } finally {
      await api.dispose();
    }
  });

  test("khách A thanh toán nhưng IPN đến sau khi giữ ghế hết hạn và khách B đã giữ ghế đó thì không được xác nhận đơn, không cướp ghế của B", async ({
    request,
  }) => {
    test.setTimeout(LATE_IPN_TEST_TIMEOUT_MS);
    const [seat] = lateIpn.seats;
    const { customer: tokenA, customer2: tokenB } = tokens;

    const { hold: holdA, payment: paymentA } = await test.step(
      "Khách A giữ ghế và tạo giao dịch VNPay nhưng chưa trả tiền",
      async () => {
        const result = await holdAndCheckout(request, tokenA, lateIpn, seat);
        expect(await seatStatusOf(request, lateIpn, seat)).toBe(SEAT_STATUS.held);
        const detail = await getBookingDetail(request, tokenA, result.hold.bookingId);
        expect(detail.status).toBe(BOOKING_STATUS.pending);
        expect(detail.payments[0].status).toBe(PAYMENT_STATUS.pending);
        return result;
      },
    );

    await test.step("Hết hạn giữ ghế: đơn của A thành Expired, giao dịch Pending thành Failed, ghế được nhả", async () => {
      await expect
        .poll(async () => (await getBookingDetail(request, tokenA, holdA.bookingId)).status, {
          message: "đơn của khách A phải chuyển sang Expired sau khi hết hạn giữ ghế",
          timeout: SERVER_EXPIRY_WAIT_MS,
          intervals: [POLL_INTERVAL_MS],
        })
        .toBe(BOOKING_STATUS.expired);

      const detail = await getBookingDetail(request, tokenA, holdA.bookingId);
      expect(detail.payments[0].status).toBe(PAYMENT_STATUS.failed);
      await expect
        .poll(() => seatStatusOf(request, lateIpn, seat), {
          message: "ghế hết hạn giữ phải trở về Available",
          timeout: SEAT_STATE_WAIT_MS,
          intervals: [POLL_INTERVAL_MS],
        })
        .toBe(SEAT_STATUS.available);
    });

    const holdB = await test.step("Khách B giữ đúng ghế đó thành công", async () => {
      const result = await holdSeatsViaApi(request, tokenB, {
        showtimeId: lateIpn.showtime.showtimeId,
        seatIds: [seat.showtimeSeatId],
      });
      expect(result.status).toBe(BOOKING_STATUS.pending);
      expect(result.bookingId).not.toBe(holdA.bookingId);
      expect(await seatStatusOf(request, lateIpn, seat)).toBe(SEAT_STATUS.held);
      return result;
    });

    await test.step("IPN thành công của khách A đến trễ: backend vẫn trả 00 (đã nhận)", async () => {
      const response = await sendIpn(
        request,
        signIpn({ txnRef: paymentA.txnRef, amount: paymentA.amount }),
      );
      expect(response.status).toBe(200);
      expect(response.rspCode).toBe(IPN_CONFIRMED_CODE);
    });

    await test.step("Đơn của A không được xác nhận: không có vé, tiền không bị bỏ sót (Success chờ xử lý hoặc đã Refunded)", async () => {
      const detail = await getBookingDetail(request, tokenA, holdA.bookingId);
      expect(detail.status).not.toBe(BOOKING_STATUS.confirmed);
      expect(detail.status).toBe(BOOKING_STATUS.expired);
      expect(detail.tickets).toHaveLength(0);

      const payment = detail.payments.find((p) => p.paymentId === paymentA.paymentId);
      expect(payment).toBeDefined();
      // Hoàn tiền tự động gọi VNPay thật: thành công -> Refunded; sandbox/mạng không cho -> giữ Success để admin xử lý tay.
      expect([PAYMENT_STATUS.success, PAYMENT_STATUS.refunded]).toContain(payment!.status);
      if (payment!.status === PAYMENT_STATUS.refunded) {
        expect(payment!.refundedAmount).toBe(paymentA.amount);
      }
    });

    await test.step("Ghế vẫn do khách B giữ: không bị Booked cho A, không bị nhả", async () => {
      expect(await seatStatusOf(request, lateIpn, seat)).toBe(SEAT_STATUS.held);
      const detailB = await getBookingDetail(request, tokenB, holdB.bookingId);
      expect(detailB.status).toBe(BOOKING_STATUS.pending);
      expect(detailB.holdExpiresAt).toBeTruthy();
      expect(detailB.tickets).toHaveLength(0);
    });

    await test.step("Khách B thanh toán thành công: nhận đúng ghế đó, ghế Booked, đơn của A vẫn không có vé", async () => {
      const paymentB = await checkoutViaApi(request, tokenB, holdB.bookingId);
      const response = await sendIpn(
        request,
        signIpn({ txnRef: paymentB.txnRef, amount: paymentB.amount }),
      );
      expect(response.rspCode).toBe(IPN_CONFIRMED_CODE);

      const detailB = await getBookingDetail(request, tokenB, holdB.bookingId);
      expect(detailB.status).toBe(BOOKING_STATUS.confirmed);
      expect(detailB.tickets).toHaveLength(1);
      expect(detailB.tickets[0]).toMatchObject({
        seatRow: seat.seatRow,
        seatNumber: seat.seatNumber,
      });
      expect(await seatStatusOf(request, lateIpn, seat)).toBe(SEAT_STATUS.booked);

      const detailA = await getBookingDetail(request, tokenA, holdA.bookingId);
      expect(detailA.status).toBe(BOOKING_STATUS.expired);
      expect(detailA.tickets).toHaveLength(0);
    });
  });
});
