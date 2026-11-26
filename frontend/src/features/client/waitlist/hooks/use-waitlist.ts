import { useEffect, useRef } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/api/query-keys";
import { toast } from "@/components/ui/toast";
import { WAITLIST_POLL_INTERVAL_MS } from "@/config/default-constants";
import { WaitlistStatus } from "@/types/enum";
import { waitlistApi } from "../api/waitlist-api";
import type { RegisterWaitlistRequest, WaitlistEntryDto } from "../types";
import { isWaitlistPolling } from "../utils/waitlist-status";

export function useMyWaitlistEntry(showtimeId: number, enabled = true) {
  return useQuery({
    queryKey: queryKeys.waitlist.mine(showtimeId),
    queryFn: () => waitlistApi.getMine(showtimeId),
    enabled: enabled && showtimeId > 0,
    retry: false,
    meta: { skipErrorToast: true },
    refetchInterval: (query) =>
      isWaitlistPolling(query.state.data?.data?.status) ? WAITLIST_POLL_INTERVAL_MS : false,
    select: (res) => res.data,
  });
}

export function useRegisterWaitlist(showtimeId: number) {
  return useMutation({
    meta: { invalidates: "waitlistChanged" },
    mutationFn: (body: RegisterWaitlistRequest) => waitlistApi.register(showtimeId, body),
  });
}

export function useWithdrawWaitlist(showtimeId: number) {
  return useMutation({
    meta: { invalidates: "waitlistChanged" },
    mutationFn: () => waitlistApi.withdraw(showtimeId),
  });
}

export function useWaitlistNoticeToast(entry: WaitlistEntryDto | undefined) {
  const previousStatus = useRef<WaitlistStatus | undefined>(undefined);
  const status = entry?.status;

  useEffect(() => {
    if (previousStatus.current === WaitlistStatus.Waiting && status === WaitlistStatus.Notified) {
      toast.add({
        title: "Đã có ghế cho suất bạn đang chờ.",
        description: "Giữ ghế ngay trong thời gian ưu tiên.",
        type: "success",
      });
    }
    previousStatus.current = status;
  }, [status]);
}
