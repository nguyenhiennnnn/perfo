import { GiftCardCodeBox } from "@/components/common/gift-card-code-box";
import {
  useApplyGiftCard,
  useGiftCardBalance,
  useRemoveGiftCard,
} from "@/features/client/gift-cards/hooks/use-gift-cards";
import type { OrderGiftCardResponse } from "@/features/client/gift-cards/types";
import type { OrderDetailDto } from "../../types";

interface BookingGiftCardSectionProps {
  order: OrderDetailDto;
  cover: OrderGiftCardResponse | null;
  onCoverChange: (cover: OrderGiftCardResponse) => void;
  onFullyCovered: () => void;
  disabled?: boolean;
}

export const BookingGiftCardSection = ({
  order,
  cover,
  onCoverChange,
  onFullyCovered,
  disabled,
}: BookingGiftCardSectionProps) => {
  const balance = useGiftCardBalance();
  const apply = useApplyGiftCard(order.orderId);
  const remove = useRemoveGiftCard(order.orderId);

  return (
    <GiftCardCodeBox
      inputId="booking-gift-card-code"
      cards={cover?.cards ?? []}
      dueAmount={cover ? cover.payableAmount : order.payment.outstandingAmount}
      disabled={disabled}
      onLookup={async (code) => (await balance.mutateAsync(code)).data}
      onApply={async (code) => {
        const res = await apply.mutateAsync(code);
        onCoverChange(res.data);
        if (res.data.isFullyCovered) onFullyCovered();
      }}
      onRemove={async (giftCardId) => {
        const res = await remove.mutateAsync(giftCardId);
        onCoverChange(res.data);
      }}
    />
  );
};
