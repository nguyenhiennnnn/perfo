import { useMutation, useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/api/query-keys";
import { surveysApi } from "../api/surveys-api";
import type { SubmitExperienceSurveyRequest } from "../types";

export function useMyExperienceSurvey(showtimeId: number, enabled = true) {
  return useQuery({
    queryKey: queryKeys.surveys.mine(showtimeId),
    queryFn: () => surveysApi.getMine(showtimeId),
    enabled: enabled && showtimeId > 0,
    retry: false,
    meta: { skipErrorToast: true },
    select: (res) => res.data,
  });
}

export function useSubmitExperienceSurvey(showtimeId: number) {
  return useMutation({
    meta: { invalidates: "surveyChanged" },
    mutationFn: (body: SubmitExperienceSurveyRequest) => surveysApi.submit(showtimeId, body),
  });
}
