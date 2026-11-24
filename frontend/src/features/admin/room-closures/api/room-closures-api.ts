import { apiDelete, apiGet, apiPost } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse, OffsetPageResult } from "@/types/api";
import type {
  AffectedShowtimesParams,
  AffectedShowtimesResponse,
  CreateRoomClosureRequest,
  DeleteRoomClosureResponse,
  RoomClosureDto,
  RoomClosureListParams,
} from "../types";

export const roomClosuresApi = {
  getClosures: (params?: RoomClosureListParams) =>
    apiGet<ApiResponse<OffsetPageResult<RoomClosureDto>>>(
      ENDPOINTS.admin.roomClosures.root,
      { params },
    ),

  getClosure: (id: number) =>
    apiGet<ApiResponse<RoomClosureDto>>(ENDPOINTS.admin.roomClosures.byId(id)),

  createClosure: (body: CreateRoomClosureRequest) =>
    apiPost<ApiResponse<RoomClosureDto>>(ENDPOINTS.admin.roomClosures.root, body),

  deleteClosure: (id: number) =>
    apiDelete<ApiResponse<DeleteRoomClosureResponse>>(
      ENDPOINTS.admin.roomClosures.byId(id),
    ),

  getClosureAffectedShowtimes: (id: number) =>
    apiGet<ApiResponse<AffectedShowtimesResponse>>(
      ENDPOINTS.admin.roomClosures.affectedShowtimes(id),
    ),

  previewAffectedShowtimes: (params: AffectedShowtimesParams) =>
    apiGet<ApiResponse<AffectedShowtimesResponse>>(
      ENDPOINTS.admin.roomClosures.affectedPreview,
      { params },
    ),
};
