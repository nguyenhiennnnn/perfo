import { useState } from "react";
import { CheckCircle2, Clock, Loader2 } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { BookingPaymentMethod } from "@/features/client/orders/components/booking/booking-payment-method";
import type { OrderGiftCardResponse } from "@/features/client/gift-cards/types";
import { formatCurrency, formatDateTime } from "@/lib/formatters";
import { PaymentMethod, RoomRentalPaymentStage } from "@/types/enum";
import {
  useApplyRoomRentalGiftCard,
  useRoomRentalCheckout,
} from "../hooks/use-room-rental-tracking";
import type { RoomRentalPaymentStageDto } from "../types";
import { ROOM_RENTAL_STAGE_LABELS } from "../utils/room-rental-labels";
import { RoomRentalGiftCardForm } from "./room-rental-gift-card-form";

interface RoomRentalPaymentStageCardProps {
  stage: RoomRentalPaymentStageDto;
  token: string;
  isActionable: boolean;
  isConfirming: boolean;
  onPaymentSubmitted: (stage: RoomRentalPaymentStage) => void;
}

export const RoomRentalPaymentStageCard = ({
  stage,
  token,
  isActionable,
  isConfirming,
  onPaymentSubmitted,
}: RoomRentalPaymentStageCardProps) => {
  const [paymentMethod, setPaymentMethod] = useState<PaymentMethod>(PaymentMethod.VNPay);
  const [cover, setCover] = useState<OrderGiftCardResponse | null>(null);
  const checkout = useRoomRentalCheckout(token);
  const applyGiftCard = useApplyRoomRentalGiftCard(token);

  const outstanding = stage.order?.outstandingAmount ?? stage.amount;
  const payable = cover ? cover.payableAmount : outstanding;
  const isCovered = cover?.isFullyCovered === true;
  const showControls = isActionable && !stage.isPaid;
  const label = ROOM_RENTAL_STAGE_LABELS[stage.stage];

  const handlePay = () =>
    checkout.mutate(
      { stage: stage.stage, paymentMethod },
      {
        onSuccess: (paymentUrl) => {
          onPaymentSubmitted(stage.stage);
          window.location.href = paymentUrl;
        },
      },
    );

  const handleApplyGiftCard = async (code: string) => {
    const res = await applyGiftCard.mutateAsync({ stage: stage.stage, code });
    setCover(res.data);
    if (res.data.isFullyCovered) onPaymentSubmitted(stage.stage);
    return res.data;
  };

  return (
    <article
      className="flex flex-col gap-3 border border-border p-4"
      aria-label={label}
      data-testid={`room-rental-payment-${stage.stage}`}
    >
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <h3 className="text-sm font-semibold text-foreground">{label}</h3>
          {stage.dueAt && (
            <p className="text-xs text-muted-foreground">Hạn thanh toán: {formatDateTime(stage.dueAt)}</p>
          )}
        </div>
        <div className="flex shrink-0 flex-col items-end gap-1">
          <span className="tabular text-base font-bold text-foreground">
            {formatCurrency(stage.amount)}
          </span>
          {stage.isPaid ? (
            <Badge variant="success">
              <CheckCircle2 className="size-3" aria-hidden />
              Đã thanh toán
            </Badge>
          ) : (
            <Badge variant={showControls ? "warning" : "secondary"}>
              {showControls ? "Chờ thanh toán" : "Chưa đến đợt này"}
            </Badge>
          )}
        </div>
      </div>

      {!stage.isPaid && !showControls && stage.stage === RoomRentalPaymentStage.Balance && (
        <p className="flex items-start gap-1.5 text-sm text-muted-foreground">
          <Clock className="mt-0.5 size-4 shrink-0" aria-hidden />
          <span>Đợt này mở sau khi bạn hoàn tất tiền cọc.</span>
        </p>
      )}

      {showControls && isConfirming && (
        <p
          role="status"
          className="flex items-start gap-1.5 bg-secondary p-3 text-sm text-foreground"
        >
          <Loader2 className="mt-0.5 size-4 shrink-0 animate-spin" aria-hidden />
          <span>Đang xác nhận thanh toán. Trạng thái sẽ tự cập nhật trong giây lát.</span>
        </p>
      )}

      {showControls && !isCovered && (
        <>
          <BookingPaymentMethod value={paymentMethod} onChange={setPaymentMethod} />
          <RoomRentalGiftCardForm
            inputId={`room-rental-gift-card-${stage.stage}`}
            cover={cover}
            disabled={checkout.isPending}
            onApply={handleApplyGiftCard}
          />
          <Button
            type="button"
            className="h-11 w-full sm:h-9"
            disabled={checkout.isPending || payable <= 0}
            onClick={handlePay}
          >
            {checkout.isPending
              ? "Đang chuyển đến cổng thanh toán..."
              : `Thanh toán ${formatCurrency(payable)}`}
          </Button>
          <p className="text-xs text-muted-foreground">
            Muốn chuyển khoản? Liên hệ rạp, nhân viên sẽ ghi nhận khoản thanh toán cho bạn.
          </p>
        </>
      )}

      {showControls && isCovered && (
        <p className="flex items-start gap-1.5 bg-success/10 p-3 text-sm text-success">
          <CheckCircle2 className="mt-0.5 size-4 shrink-0" aria-hidden />
          <span>Thẻ quà tặng đã đủ để thanh toán đợt này. Đang chờ xác nhận.</span>
        </p>
      )}
    </article>
  );
};
