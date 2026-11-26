import { apiClient, apiGet, apiPost } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ScreenTypeDto } from "@/features/admin/catalog/types";
import { EXPORT_TIMEOUT_MS } from "@/config/default-constants";
import type { OrderGiftCardResponse } from "@/features/client/gift-cards/types";
import type { CheckoutResponse } from "@/features/client/orders/types";
import { getFilenameFromContentDisposition, normalizeBlobError } from "@/lib/download";
import type { ApiResponse, OffsetPageResult } from "@/types/api";
import type { PaymentMethod, RoomRentalPaymentStage } from "@/types/enum";
import type {
  CreateRoomRentalRequestRequest,
  RoomRentalPaymentsDto,
  RoomRentalRequestReceiptDto,
  RoomRentalTrackingDto,
  RoomRentalTrackingQuoteDecisionDto,
  RoomRentalTrackingQuotesDto,
} from "../types";

const requests = ENDPOINTS.roomRentalRequests;

export const roomRentalsApi = {
  create: (body: CreateRoomRentalRequestRequest) =>
    apiPost<ApiResponse<RoomRentalRequestReceiptDto>>(
      ENDPOINTS.roomRentalRequests.root,
      body,
    ),

  getScreenTypes: () =>
    apiGet<ApiResponse<OffsetPageResult<ScreenTypeDto>>>(ENDPOINTS.catalog.screenTypes),

  getTracking: (token: string) =>
    apiPost<ApiResponse<RoomRentalTrackingDto>>(requests.tracking, { token }),

  getTrackingQuotes: (token: string) =>
    apiPost<ApiResponse<RoomRentalTrackingQuotesDto>>(requests.trackingQuotes, { token }),

  downloadQuotePdf: async (token: string, version: number, fallbackFilename: string) => {
    try {
      const response = await apiClient.post<Blob>(
        requests.trackingQuotePdf(version),
        { token },
        { responseType: "blob", timeout: EXPORT_TIMEOUT_MS },
      );
      return {
        blob: response.data,
        filename:
          getFilenameFromContentDisposition(response.headers["content-disposition"]) ??
          fallbackFilename,
      };
    } catch (error) {
      throw await normalizeBlobError(error);
    }
  },

  acceptQuote: (token: string, version: number) =>
    apiPost<ApiResponse<RoomRentalTrackingQuoteDecisionDto>>(
      requests.trackingQuoteAccept(version),
      { token },
    ),

  declineQuote: (token: string, version: number, reason: string | null) =>
    apiPost<ApiResponse<RoomRentalTrackingQuoteDecisionDto>>(
      requests.trackingQuoteDecline(version),
      { token, reason },
    ),

  getTrackingPayments: (token: string) =>
    apiPost<ApiResponse<RoomRentalPaymentsDto>>(requests.trackingPayments, { token }),

  checkoutPayment: (token: string, stage: RoomRentalPaymentStage, paymentMethod: PaymentMethod) =>
    apiPost<ApiResponse<CheckoutResponse>>(requests.trackingPaymentCheckout(stage), {
      token,
      paymentMethod,
    }),

  applyPaymentGiftCard: (token: string, stage: RoomRentalPaymentStage, code: string) =>
    apiPost<ApiResponse<OrderGiftCardResponse>>(requests.trackingPaymentGiftCard(stage), {
      token,
      code,
    }),
};
