import { apiGet, apiPost } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ScreenTypeDto } from "@/features/admin/catalog/types";
import type { ApiResponse, OffsetPageResult } from "@/types/api";
import type {
  CreateRoomRentalRequestRequest,
  RoomRentalRequestReceiptDto,
} from "../types";

export const roomRentalsApi = {
  create: (body: CreateRoomRentalRequestRequest) =>
    apiPost<ApiResponse<RoomRentalRequestReceiptDto>>(
      ENDPOINTS.roomRentalRequests.root,
      body,
    ),

  getScreenTypes: () =>
    apiGet<ApiResponse<OffsetPageResult<ScreenTypeDto>>>(ENDPOINTS.catalog.screenTypes),
};
