import { apiGet, apiPost } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse } from "@/types/api";
import type { ExperienceSurveyDto, SubmitExperienceSurveyRequest } from "../types";

export const surveysApi = {
  getMine: (showtimeId: number) =>
    apiGet<ApiResponse<ExperienceSurveyDto>>(ENDPOINTS.experienceSurveys.byShowtime(showtimeId)),

  submit: (showtimeId: number, body: SubmitExperienceSurveyRequest) =>
    apiPost<ApiResponse<ExperienceSurveyDto>>(ENDPOINTS.experienceSurveys.byShowtime(showtimeId), body),
};
