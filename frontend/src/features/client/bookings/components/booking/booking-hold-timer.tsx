import { AlertTriangle, Clock } from "lucide-react";
import { cn } from "@/lib/utils";
import { useCountdown } from "@/hooks/use-countdown";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";

const WARN_MS = 120_000;
const URGENT_MS = 60_000;
const FINAL_MS = 30_000;

type HoldLevel = "normal" | "warn" | "urgent" | "final" | "expired";

function getLevel(remainingMs: number): HoldLevel {
  if (remainingMs <= 0) return "expired";
  if (remainingMs <= FINAL_MS) return "final";
  if (remainingMs <= URGENT_MS) return "urgent";
  if (remainingMs <= WARN_MS) return "warn";
  return "normal";
}

// Chỉ đọc cho screen reader khi chuyển mốc (2 phút, 1 phút, 30 giây, hết giờ), không đọc mỗi giây.
// Cố ý không chứa cụm "Giữ ghế còn" để getByText(/Giữ ghế còn/) trong e2e chỉ khớp đúng một phần tử.
const ANNOUNCEMENTS: Record<HoldLevel, string> = {
  normal: "",
  warn: "Còn dưới 2 phút để thanh toán.",
  urgent: "Còn dưới 1 phút để thanh toán.",
  final: "Còn dưới 30 giây để thanh toán.",
  expired: "Đã hết thời gian giữ ghế.",
};

const HINTS: Partial<Record<HoldLevel, string>> = {
  warn: "Hoàn tất thanh toán sớm để không mất ghế.",
  urgent: "Hoàn tất thanh toán sớm để không mất ghế.",
  final: "Hoàn tất thanh toán sớm để không mất ghế.",
  expired: "Đã hết thời gian giữ ghế.",
};

const TONES: Record<HoldLevel, string> = {
  normal: "border border-border text-warning-text",
  warn: "bg-warning/10 text-warning-text",
  urgent: "bg-destructive/10 text-destructive",
  final: "bg-destructive/10 text-destructive",
  expired: "bg-destructive/10 text-destructive",
};

export const BookingHoldTimer = ({ expiresAt, className }: { expiresAt: string; className?: string }) => {
  const { label, remainingMs } = useCountdown(expiresAt);
  const level = getLevel(remainingMs);
  const isAlert = level === "urgent" || level === "final" || level === "expired";
  const Icon = isAlert ? AlertTriangle : Clock;
  const hint = HINTS[level];

  return (
    <Tooltip>
      <TooltipTrigger
        render={
          // Lớp ngoài giữ nền đặc để nội dung cuộn phía sau không lộ qua khi sticky;
          // lớp trong đổi tông theo mức khẩn cấp. Cảnh báo không chỉ dựa vào màu: đổi icon và có dòng chữ.
          <div role="timer" className={cn("bg-background", className)}>
            <div className={cn("flex flex-col items-center gap-0.5 px-4 py-2.5", TONES[level])}>
              <div className="flex items-center gap-2">
                <Icon className="size-5 shrink-0" aria-hidden />
                <span className="text-sm font-medium">Giữ ghế còn</span>
                <span className="t-code text-2xl font-bold tabular-nums">{label}</span>
              </div>
              {hint && <p className="text-sm font-medium">{hint}</p>}
              <span className="sr-only" role="status" aria-live="polite">
                {ANNOUNCEMENTS[level]}
              </span>
            </div>
          </div>
        }
      />
      <TooltipContent>Ghế sẽ tự động được giải phóng nếu bạn chưa hoàn tất thanh toán trước khi hết giờ.</TooltipContent>
    </Tooltip>
  );
}
