import { useEffect, useState } from "react";
import { NOW_TICK_INTERVAL_MS } from "@/config/default-constants";
import { getServerNow } from "@/lib/server-clock";

export function useNow(intervalMs = NOW_TICK_INTERVAL_MS, enabled = true) {
  const [now, setNow] = useState(() => getServerNow());
  const [wasEnabled, setWasEnabled] = useState(enabled);

  if (wasEnabled !== enabled) {
    setWasEnabled(enabled);
    if (enabled) setNow(getServerNow());
  }

  useEffect(() => {
    if (!enabled) return;
    const id = setInterval(() => setNow(getServerNow()), intervalMs);
    return () => clearInterval(id);
  }, [intervalMs, enabled]);

  return now;
}
