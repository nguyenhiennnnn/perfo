import { Clock } from "lucide-react";
import { ButtonLink } from "@/components/common/button-link";
import { formatDate } from "@/lib/formatters";

interface MembershipExpiryBannerProps {
  planName: string;
  validTo: string;
  daysLeft: number;
  renewTo: string;
}

export const MembershipExpiryBanner = ({
  planName,
  validTo,
  daysLeft,
  renewTo,
}: MembershipExpiryBannerProps) => (
  <div
    role="status"
    className="flex flex-wrap items-center justify-between gap-3 cut-md bg-warning/10 p-4"
  >
    <div>
      <p className="flex items-center gap-2 text-sm font-medium text-warning-text">
        <Clock className="size-4 shrink-0" aria-hidden />
        {daysLeft > 0
          ? `Gói ${planName} sẽ hết hạn sau ${daysLeft} ngày (${formatDate(validTo)}).`
          : `Gói ${planName} hết hạn hôm nay (${formatDate(validTo)}).`}
      </p>
      <p className="mt-1 text-xs text-muted-foreground">
        Gia hạn trước khi hết hạn để không gián đoạn quyền lợi. Gói mới sẽ nối tiếp ngay sau gói
        hiện tại và bạn tự thanh toán, hệ thống không tự động trừ tiền.
      </p>
    </div>
    <ButtonLink to={renewTo}>Gia hạn ngay</ButtonLink>
  </div>
);
