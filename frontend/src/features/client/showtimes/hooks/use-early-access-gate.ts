import { MS_PER_SECOND } from "@/config/default-constants";
import { useNow } from "@/hooks/use-now";

import type { ShowtimeEarlyAccessDto } from "../types";

export function useEarlyAccessGate(earlyAccess: ShowtimeEarlyAccessDto | null | undefined) {
  const now = useNow(MS_PER_SECOND, earlyAccess != null);

  if (!earlyAccess) return { isRestricted: false, isBlocked: false, remainingMs: 0 };

  const remainingMs = Math.max(0, new Date(earlyAccess.generalSaleStartTime).getTime() - now);
  const isRestricted = remainingMs > 0;

  return { isRestricted, isBlocked: isRestricted && !earlyAccess.canBook, remainingMs };
}
