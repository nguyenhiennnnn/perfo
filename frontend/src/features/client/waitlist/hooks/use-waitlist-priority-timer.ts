import { useEffect, useRef } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { queryKeys } from "@/api/query-keys";
import { useCountdown } from "@/hooks/use-countdown";
import type { WaitlistEntryDto } from "../types";

export function useWaitlistPriorityTimer(entry: WaitlistEntryDto) {
  const queryClient = useQueryClient();
  const { label, remainingMs } = useCountdown(entry.priorityExpiresAt ?? null);
  const expired = remainingMs <= 0;
  const refreshedFor = useRef<string | null>(null);

  useEffect(() => {
    if (!expired || refreshedFor.current === entry.priorityExpiresAt) return;
    refreshedFor.current = entry.priorityExpiresAt ?? null;
    void queryClient.invalidateQueries({ queryKey: queryKeys.waitlist.mine(entry.showtimeId) });
  }, [expired, entry.priorityExpiresAt, entry.showtimeId, queryClient]);

  return { label, expired };
}
