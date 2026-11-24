import { apiGet, apiPost } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse, OffsetPageResult } from "@/types/api";
import type {
  RoomChangeRefundConfirmResultDto,
  RoomChangeRefundListItemDto,
  RoomChangeRefundListParams,
} from "../types";

export const roomChangeRefundsApi = {
  getRoomChangeRefunds: (params: RoomChangeRefundListParams) =>
    apiGet<ApiResponse<OffsetPageResult<RoomChangeRefundListItemDto>>>(
      ENDPOINTS.admin.roomChangeRefunds.root,
      { params },
    ),

  confirmRoomChangeRefund: (roomChangeRefundDueId: number) =>
    apiPost<ApiResponse<RoomChangeRefundConfirmResultDto>>(
      ENDPOINTS.admin.roomChangeRefunds.confirm(roomChangeRefundDueId),
    ),
};
