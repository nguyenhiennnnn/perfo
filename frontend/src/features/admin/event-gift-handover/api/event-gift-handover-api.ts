import { apiGet, apiPost } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse } from "@/types/api";

import type { EventGiftHandoverResponse, EventGiftHandoverTicketDto } from "../types";

const paths = ENDPOINTS.admin.eventGiftHandovers;

export const eventGiftHandoverApi = {
  lookup: (qrCode: string) =>
    apiGet<ApiResponse<EventGiftHandoverTicketDto>>(paths.lookup, { params: { qrCode } }),
  handOver: (qrCode: string) =>
    apiPost<ApiResponse<EventGiftHandoverResponse>>(paths.root, { qrCode }),
};
