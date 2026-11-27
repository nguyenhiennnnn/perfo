import { MS_PER_SECOND } from "@/config/default-constants";

import type { ShowtimeEarlyAccessDto } from "../types";

const SECONDS_PER_DAY = 86_400;
const pad = (value: number) => value.toString().padStart(2, "0");

export function formatSaleCountdown(remainingMs: number): string {
  const totalSeconds = Math.floor(remainingMs / MS_PER_SECOND);
  const days = Math.floor(totalSeconds / SECONDS_PER_DAY);
  const rest = totalSeconds % SECONDS_PER_DAY;
  const clock = `${pad(Math.floor(rest / 3600))}:${pad(Math.floor((rest % 3600) / 60))}:${pad(rest % 60)}`;
  return days > 0 ? `${days} ngày ${clock}` : clock;
}

export function describeEarlyAccessGroups(earlyAccess: ShowtimeEarlyAccessDto): string[] {
  const groups: string[] = [];
  if (earlyAccess.membershipTierNames.length > 0)
    groups.push(`Hạng thành viên ${earlyAccess.membershipTierNames.join(", ")}`);
  if (earlyAccess.paidMembers) groups.push("Hội viên gói trả phí");
  if (earlyAccess.movieInterest) groups.push("Khách đã bấm quan tâm phim");
  return groups;
}
