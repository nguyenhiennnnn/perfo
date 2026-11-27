import { apiDelete, apiGet, apiPut } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse } from "@/types/api";

import type {
  ShowtimeEarlyAccessSettingDto,
  ShowtimeEventDto,
  ShowtimeEventGiftsDto,
  ShowtimeEventLimitsDto,
  UpsertShowtimeEarlyAccessRequest,
  UpsertShowtimeEventGiftsRequest,
  UpsertShowtimeEventLimitsRequest,
  UpsertShowtimeEventRequest,
} from "../types";

const paths = ENDPOINTS.admin.showtimes;

export const showtimeEventApi = {
  get: (showtimeId: number) =>
    apiGet<ApiResponse<ShowtimeEventDto>>(paths.event(showtimeId)),
  upsert: (showtimeId: number, body: UpsertShowtimeEventRequest) =>
    apiPut<ApiResponse<ShowtimeEventDto>>(paths.event(showtimeId), body),
  remove: (showtimeId: number) =>
    apiDelete<ApiResponse<number>>(paths.event(showtimeId)),

  getLimits: (showtimeId: number) =>
    apiGet<ApiResponse<ShowtimeEventLimitsDto>>(paths.eventLimits(showtimeId)),
  upsertLimits: (showtimeId: number, body: UpsertShowtimeEventLimitsRequest) =>
    apiPut<ApiResponse<ShowtimeEventLimitsDto>>(paths.eventLimits(showtimeId), body),

  getGifts: (showtimeId: number) =>
    apiGet<ApiResponse<ShowtimeEventGiftsDto>>(paths.eventGifts(showtimeId)),
  upsertGifts: (showtimeId: number, body: UpsertShowtimeEventGiftsRequest) =>
    apiPut<ApiResponse<ShowtimeEventGiftsDto>>(paths.eventGifts(showtimeId), body),

  getEarlyAccess: (showtimeId: number) =>
    apiGet<ApiResponse<ShowtimeEarlyAccessSettingDto>>(paths.eventEarlyAccess(showtimeId)),
  upsertEarlyAccess: (showtimeId: number, body: UpsertShowtimeEarlyAccessRequest) =>
    apiPut<ApiResponse<ShowtimeEarlyAccessSettingDto>>(
      paths.eventEarlyAccess(showtimeId),
      body,
    ),
  removeEarlyAccess: (showtimeId: number) =>
    apiDelete<ApiResponse<number>>(paths.eventEarlyAccess(showtimeId)),
};
