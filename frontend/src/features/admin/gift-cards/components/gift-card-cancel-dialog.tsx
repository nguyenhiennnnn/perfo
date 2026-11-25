import { useEffect, useState } from "react";

import { Chip, ChipGroup } from "@/components/common/chip";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { FORM_PROCESSING_LABEL } from "@/config/default-constants";
import { formatCurrency } from "@/lib/formatters";
import { cn } from "@/lib/utils";
import { useDialogClose, useDialogDismissGuard } from "@/stores/dialog-store";

import { useAdminCancelGiftCard } from "../hooks/use-admin-gift-cards";
import {
  GIFT_CARD_CANCEL_REASON_MAX_LENGTH,
  type AdminGiftCard,
  type GiftCardCancelBalanceAction,
} from "../types";
import { GIFT_CARD_BALANCE_ACTIONS, isGiftCardPartiallyUsed } from "../utils/labels";

interface GiftCardCancelDialogContentProps {
  giftCard: AdminGiftCard;
}

export const GiftCardCancelDialogContent = ({ giftCard }: GiftCardCancelDialogContentProps) => {
  const closeDialog = useDialogClose();
  const { block, release } = useDialogDismissGuard();
  const cancel = useAdminCancelGiftCard();

  const partiallyUsed = isGiftCardPartiallyUsed(giftCard);
  const [reason, setReason] = useState("");
  const [balanceAction, setBalanceAction] = useState<GiftCardCancelBalanceAction | null>(null);

  useEffect(() => {
    if (!cancel.isPending) return;
    block();
    return release;
  }, [cancel.isPending, block, release]);

  const trimmedReason = reason.trim();
  const reasonTooLong = reason.length > GIFT_CARD_CANCEL_REASON_MAX_LENGTH;
  const canSubmit =
    trimmedReason.length > 0 &&
    !reasonTooLong &&
    (!partiallyUsed || balanceAction !== null) &&
    !cancel.isPending;
  const selectedAction = GIFT_CARD_BALANCE_ACTIONS.find((item) => item.value === balanceAction);

  const handleSubmit = async (event: React.SyntheticEvent) => {
    event.preventDefault();
    if (!canSubmit) return;
    try {
      await cancel.mutateAsync({
        id: giftCard.giftCardId,
        body: { reason: trimmedReason, balanceAction: partiallyUsed ? balanceAction : null },
      });
      closeDialog();
    } catch {
      return;
    }
  };

  return (
    <form onSubmit={(e) => void handleSubmit(e)} className="flex flex-col gap-4">
      <div className="flex flex-col gap-1 text-sm text-muted-foreground">
        <p>
          Thẻ <span className="t-code font-medium text-foreground">{giftCard.maskedCode}</span> còn
          số dư{" "}
          <span className="font-medium tabular-nums text-foreground">
            {formatCurrency(giftCard.balance)}
          </span>
          . Hủy thẻ không thể hoàn tác.
        </p>
        {giftCard.reservedAmount > 0 ? (
          <p>
            Thẻ đang được giữ chỗ {formatCurrency(giftCard.reservedAmount)} trong đơn chờ thanh
            toán; các giữ chỗ này sẽ được nhả khi hủy.
          </p>
        ) : null}
      </div>

      {partiallyUsed ? (
        <div className="flex flex-col gap-2">
          <p id="gift-card-cancel-action-label" className="text-sm font-medium text-foreground">
            Xử lý số dư còn lại <span className="text-destructive">*</span>
          </p>
          <ChipGroup role="radiogroup" aria-labelledby="gift-card-cancel-action-label">
            {GIFT_CARD_BALANCE_ACTIONS.map((item) => (
              <Chip
                key={item.value}
                role="radio"
                selected={balanceAction === item.value}
                disabled={cancel.isPending}
                onClick={() => setBalanceAction(item.value)}
              >
                {item.label}
              </Chip>
            ))}
          </ChipGroup>
          <p className="text-xs text-muted-foreground">
            {selectedAction?.description ?? "Thẻ đã dùng một phần, chọn cách xử lý số dư còn lại."}
          </p>
        </div>
      ) : null}

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="gift-card-cancel-reason">Lý do hủy (bắt buộc)</Label>
        <Textarea
          id="gift-card-cancel-reason"
          rows={3}
          className="min-h-20 resize-none"
          value={reason}
          aria-invalid={reasonTooLong || undefined}
          disabled={cancel.isPending}
          onChange={(e) => setReason(e.target.value)}
        />
        <p className={cn("text-xs", reasonTooLong ? "text-destructive" : "text-muted-foreground")}>
          {reason.length}/{GIFT_CARD_CANCEL_REASON_MAX_LENGTH}
        </p>
      </div>

      <div className="grid grid-cols-2 gap-2">
        <Button type="button" variant="outline" onClick={closeDialog} disabled={cancel.isPending}>
          Quay lại
        </Button>
        <Button type="submit" variant="destructive" disabled={!canSubmit}>
          {cancel.isPending ? FORM_PROCESSING_LABEL : "Hủy thẻ"}
        </Button>
      </div>
    </form>
  );
};
