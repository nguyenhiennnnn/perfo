import { useNow } from "@/hooks/use-now";
import { MS_PER_SECOND } from "@/config/default-constants";

export function useCountdown(expiresAt: string | null) {
  const now = useNow(MS_PER_SECOND, expiresAt !== null);
  const remainingMs = expiresAt ? Math.max(0, new Date(expiresAt).getTime() - now) : 0;

  const totalSeconds = Math.floor(remainingMs / MS_PER_SECOND);
  return {
    remainingMs,
    label: `${Math.floor(totalSeconds / 60)}:${(totalSeconds % 60).toString().padStart(2, "0")}`,
  };
}
