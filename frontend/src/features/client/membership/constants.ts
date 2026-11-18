import {
  Award,
  Sparkles,
  Crown,
  Gem,
  Percent,
  Gift,
  CalendarCheck,
} from "lucide-react";
import { formatCurrency } from "@/lib/formatters";

export const TIER_VISUALS = [
  {
    icon: Award,
    accent: "text-warning-text",
    ring: "hover:border-warning/50",
    perk: "Quyền lợi hội viên cơ bản",
  },
  {
    icon: Sparkles,
    accent: "text-muted-foreground",
    ring: "hover:border-muted-foreground/40",
    perk: "Chiết khấu ưu đãi vé & bắp nước",
  },
  {
    icon: Crown,
    accent: "text-accent-text",
    ring: "hover:border-accent/50",
    perk: "Ưu tiên giữ chỗ sự kiện đặc biệt",
  },
  {
    icon: Gem,
    accent: "text-primary-text",
    ring: "hover:border-primary/50",
    perk: "Tặng combo bắp nước miễn phí dịp sinh nhật",
  },
] as const;

export const TIER_SKELETON_COUNT = 4;

export const TIER_ROLE_LAYOUT = {
  first: { shape: "cut-sm", pad: "p-4", number: "text-4xl", weight: "1fr" },
  middle: { shape: "cut-md", pad: "p-5", number: "text-5xl", weight: "1.1fr" },
  featured: { shape: "cut-lg", pad: "p-6 md:p-8", number: "text-6xl md:text-7xl", weight: "1.6fr" },
  last: { shape: "cut-md", pad: "p-5 md:p-6", number: "text-5xl", weight: "1.2fr" },
} as const;

export const COMPARE_ROWS = [
  { label: "Tích điểm mỗi giao dịch", unlockAt: 0, icon: Sparkles },
  { label: "Chiết khấu trên giá vé (không áp dụng combo)", unlockAt: 1, icon: Percent },
  { label: "Ưu tiên giữ chỗ sự kiện", unlockAt: 2, icon: CalendarCheck },
  { label: "Combo bắp nước miễn phí sinh nhật", unlockAt: 3, icon: Gift },
] as const;

export const getFaqItems = (pointsPerAmount?: number | null, loyaltyWindowMonths?: number | null) => {
  const spendText = pointsPerAmount ? formatCurrency(pointsPerAmount) : "mức chi tiêu do rạp quy định";
  const windowText = loyaltyWindowMonths ? `${loyaltyWindowMonths} tháng` : "thời hạn do rạp quy định";

  return [
    {
      question: "Làm sao để tích điểm hội viên?",
      answer: `Mỗi ${spendText} chi tiêu cho vé xem phim hoặc bắp nước tại rạp (thanh toán thành công) sẽ được cộng 1 điểm thưởng vào tài khoản của bạn. Điểm được cộng tự động ngay sau khi đơn hàng hoàn tất.`,
    },
    {
      question: "Điểm và hạng thẻ của tôi có bị hết hạn không?",
      answer: `Điểm có hiệu lực trong ${windowText} kể từ ngày tích. Hạng thẻ luôn được xét theo tổng điểm còn hiệu lực; khi điểm cũ hết hạn và tổng điểm không còn đủ ngưỡng, tài khoản sẽ tự động điều chỉnh về hạng phù hợp.`,
    },
    {
      question: "Hủy vé thì điểm thưởng đã tích có bị trừ không?",
      answer:
        "Khi vé được hủy và hoàn tiền thành công, số điểm thưởng tương ứng với giá trị đơn hàng đó sẽ được trừ lại khỏi tổng điểm tích lũy của bạn.",
    },
  ];
};
