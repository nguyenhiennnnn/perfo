import { apiGet, apiPost } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse } from "@/types/api";

import type {
  BlockSeatsRequest,
  SeatBlockDto,
  UnblockSeatsRequest,
  UnblockSeatsResponse,
} from "../types";

export const seatBlocksApi = {
  getSeatBlocks: (showtimeId: number) =>
    apiGet<ApiResponse<SeatBlockDto[]>>(
      ENDPOINTS.admin.showtimes.seatBlocks(showtimeId),
    ),

  blockSeats: (showtimeId: number, body: BlockSeatsRequest) =>
    apiPost<ApiResponse<SeatBlockDto[]>>(
      ENDPOINTS.admin.showtimes.seatBlocks(showtimeId),
      body,
    ),

  unblockSeats: (showtimeId: number, body: UnblockSeatsRequest) =>
    apiPost<ApiResponse<UnblockSeatsResponse>>(
      ENDPOINTS.admin.showtimes.seatBlocksUnblock(showtimeId),
      body,
    ),
};
