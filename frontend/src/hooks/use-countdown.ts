import { useEffect, useState } from "react";
import { getServerNow } from "@/lib/server-clock";
import { MS_PER_SECOND } from "@/config/default-constants";

export function useCountdown(expiresAt: string | null) {
  const [remainingMs, setRemainingMs] = useState(0);

  useEffect(() => {
    if (!expiresAt) {
      setRemainingMs(0);
      return;
    }
    const target = new Date(expiresAt).getTime();
    const tick = () => setRemainingMs(Math.max(0, target - getServerNow()));
    tick();
    const interval = setInterval(tick, MS_PER_SECOND);
    return () => clearInterval(interval);
  }, [expiresAt]);

  const totalSeconds = Math.floor(remainingMs / MS_PER_SECOND);
  return {
    remainingMs,
    label: `${Math.floor(totalSeconds / 60)}:${(totalSeconds % 60).toString().padStart(2, "0")}`,
  };
}
