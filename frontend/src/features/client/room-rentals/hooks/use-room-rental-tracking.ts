import { useMutation, useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/api/query-keys";
import { BOOKING_RESULT_POLL_INTERVAL_MS } from "@/config/default-constants";
import { saveBlob } from "@/lib/download";
import type { PaymentMethod, RoomRentalPaymentStage } from "@/types/enum";
import { roomRentalsApi } from "../api/room-rentals-api";
import { saveRoomRentalPaymentReturn } from "../utils/room-rental-payment-return";

export function useRoomRentalTracking(token: string, polling: boolean) {
  return useQuery({
    queryKey: queryKeys.roomRentals.tracking(token),
    queryFn: () => roomRentalsApi.getTracking(token),
    select: (res) => res.data,
    enabled: token.length > 0,
    retry: false,
    refetchOnWindowFocus: false,
    refetchInterval: polling ? BOOKING_RESULT_POLL_INTERVAL_MS : false,
    meta: { skipErrorToast: true },
  });
}

export function useRoomRentalTrackingQuotes(token: string, enabled: boolean) {
  return useQuery({
    queryKey: queryKeys.roomRentals.trackingQuotes(token),
    queryFn: () => roomRentalsApi.getTrackingQuotes(token),
    select: (res) => res.data,
    enabled,
    retry: false,
    refetchOnWindowFocus: false,
    meta: { skipErrorToast: true },
  });
}

export function useRoomRentalTrackingPayments(token: string, enabled: boolean, polling: boolean) {
  return useQuery({
    queryKey: queryKeys.roomRentals.trackingPayments(token),
    queryFn: () => roomRentalsApi.getTrackingPayments(token),
    select: (res) => res.data,
    enabled,
    retry: false,
    refetchOnWindowFocus: false,
    refetchInterval: polling ? BOOKING_RESULT_POLL_INTERVAL_MS : false,
    meta: { skipErrorToast: true },
  });
}

export function useAcceptRoomRentalQuote(token: string) {
  return useMutation({
    meta: { invalidates: "roomRentalTrackingChanged" },
    mutationFn: (version: number) => roomRentalsApi.acceptQuote(token, version),
  });
}

interface DeclineQuoteVariables {
  version: number;
  reason: string | null;
}

export function useDeclineRoomRentalQuote(token: string) {
  return useMutation({
    meta: { invalidates: "roomRentalTrackingChanged" },
    mutationFn: ({ version, reason }: DeclineQuoteVariables) =>
      roomRentalsApi.declineQuote(token, version, reason),
  });
}

export function useDownloadRoomRentalQuotePdf(token: string) {
  return useMutation({
    meta: { errorTitle: "Không thể tải báo giá PDF", skipSuccessToast: true },
    mutationFn: async (version: number) => {
      const { blob, filename } = await roomRentalsApi.downloadQuotePdf(
        token,
        version,
        `bao-gia-v${version}.pdf`,
      );
      saveBlob(blob, filename);
    },
  });
}

interface CheckoutVariables {
  stage: RoomRentalPaymentStage;
  paymentMethod: PaymentMethod;
}

export function useRoomRentalCheckout(token: string) {
  return useMutation({
    meta: { skipSuccessToast: true, errorTitle: "Không thể chuyển đến cổng thanh toán" },
    mutationFn: async ({ stage, paymentMethod }: CheckoutVariables) => {
      const checkout = await roomRentalsApi.checkoutPayment(token, stage, paymentMethod);
      try {
        const payments = await roomRentalsApi.getTrackingPayments(token);
        const order = payments.data.stages.find((item) => item.stage === stage)?.order;
        if (order) saveRoomRentalPaymentReturn({ orderId: order.orderId, token, stage });
      } catch {
        return checkout.data.paymentUrl;
      }
      return checkout.data.paymentUrl;
    },
  });
}

interface ApplyGiftCardVariables {
  stage: RoomRentalPaymentStage;
  code: string;
}

export function useApplyRoomRentalGiftCard(token: string) {
  return useMutation({
    meta: { invalidates: "roomRentalTrackingChanged", skipErrorToast: true },
    mutationFn: ({ stage, code }: ApplyGiftCardVariables) =>
      roomRentalsApi.applyPaymentGiftCard(token, stage, code),
  });
}
