import { useMutation, useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/api/query-keys";
import { verificationApi } from "../api/verification-api";

export function useVerificationStatus(enabled: boolean) {
  return useQuery({
    queryKey: queryKeys.verification.status(),
    queryFn: () => verificationApi.getStatus(),
    enabled,
    meta: { skipErrorToast: true },
    select: (res) => res.data,
  });
}

export function useSendVerificationCode() {
  return useMutation({
    meta: { invalidates: "verificationChanged" },
    mutationFn: () => verificationApi.sendCode(),
  });
}

export function useConfirmVerification() {
  return useMutation({
    meta: { invalidates: "verificationChanged" },
    mutationFn: (code: string) => verificationApi.confirm({ code }),
  });
}
