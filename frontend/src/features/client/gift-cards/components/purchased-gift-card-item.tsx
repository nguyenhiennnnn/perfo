import { CalendarClock, Gift } from "lucide-react";
import { GiftCardStatusBadge } from "@/components/common/status-badge";
import { formatCurrency, formatDateTime } from "@/lib/formatters";
import { GiftCardStatus } from "@/types/enum";
import type { PurchasedGiftCardDto } from "../types";

const getDeliveryLine = (card: PurchasedGiftCardDto) => {
  switch (card.status) {
    case GiftCardStatus.PendingSend:
      return card.sendAt ? `Sẽ gửi lúc ${formatDateTime(card.sendAt)}` : "Đang chờ gửi";
    case GiftCardStatus.Sent:
      return card.recipientEmail ? `Đã gửi tới ${card.recipientEmail}` : "Đã gửi";
    case GiftCardStatus.InUse:
      return "Người nhận đang sử dụng";
    case GiftCardStatus.Depleted:
      return "Đã dùng hết số dư";
    case GiftCardStatus.Expired:
      return "Thẻ đã hết hạn";
    case GiftCardStatus.Cancelled:
      return "Thẻ đã bị hủy";
  }
};

interface PurchasedGiftCardItemProps {
  card: PurchasedGiftCardDto;
}

export const PurchasedGiftCardItem = ({ card }: PurchasedGiftCardItemProps) => (
  <article className="flex gap-4 cut-md bg-card p-4">
    <div className="flex size-11 shrink-0 items-center justify-center cut-xs bg-primary/10 text-primary-text">
      <Gift className="size-5" aria-hidden />
    </div>

    <div className="min-w-0 flex-1 space-y-2">
      <div className="flex flex-wrap items-start justify-between gap-2">
        <div className="min-w-0">
          <p className="t-h3 text-foreground">{formatCurrency(card.faceValue)}</p>
          <p className="t-code text-xs text-muted-foreground">{card.maskedCode}</p>
        </div>
        <GiftCardStatusBadge status={card.status} />
      </div>

      <p className="text-sm text-foreground">
        Người nhận:{" "}
        <span className="font-medium">
          {card.recipientName || card.recipientEmail || "—"}
        </span>
      </p>

      <p className="flex items-center gap-1.5 text-sm text-muted-foreground">
        <CalendarClock className="size-4 shrink-0" aria-hidden />
        {getDeliveryLine(card)}
      </p>

      <div className="flex flex-wrap gap-x-4 gap-y-1 text-xs text-muted-foreground">
        {card.status === GiftCardStatus.InUse && (
          <span>
            Số dư:{" "}
            <span className="font-semibold text-primary-text">{formatCurrency(card.balance)}</span>
          </span>
        )}
        <span>Hạn dùng: {formatDateTime(card.expiresAt)}</span>
      </div>

      {card.message && (
        <p className="line-clamp-2 border-l-2 border-border pl-3 text-sm italic text-muted-foreground">
          {card.message}
        </p>
      )}
    </div>
  </article>
);
