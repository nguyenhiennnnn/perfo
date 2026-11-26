import { MS_PER_SECOND } from "@/config/default-constants";

const SECONDS_PER_MINUTE = 60;
const MINUTES_PER_HOUR = 60;

export function formatRemaining(remainingMs: number): string {
  const totalSeconds = Math.max(0, Math.ceil(remainingMs / MS_PER_SECOND));
  const totalMinutes = Math.floor(totalSeconds / SECONDS_PER_MINUTE);
  const seconds = totalSeconds % SECONDS_PER_MINUTE;
  if (totalMinutes >= MINUTES_PER_HOUR) {
    const hours = Math.floor(totalMinutes / MINUTES_PER_HOUR);
    const minutes = totalMinutes % MINUTES_PER_HOUR;
    return minutes > 0 ? `${hours} giờ ${minutes} phút` : `${hours} giờ`;
  }
  if (totalMinutes > 0) return `${totalMinutes} phút ${seconds} giây`;
  return `${seconds} giây`;
}
