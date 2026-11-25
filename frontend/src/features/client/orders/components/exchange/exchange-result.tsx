import type { ReactNode } from "react";
import { CheckCircle2, Gift, HandCoins } from "lucide-react";
import { ButtonLink } from "@/components/common/button-link";
import { CopyButton } from "@/components/common/copy-button";
import { ROUTES } from "@/config/routes";
import { formatCurrency, formatDate } from "@/lib/formatters";
import type { ExchangeChannel, StartOrderExchangeResponse } from "../../types";
import { isCashRefundKind, isCreditKind } from "../../utils/order-exchange";

interface ExchangeResultProps {
  result: StartOrderExchangeResponse;
  channel: ExchangeChannel;
}

const SettlementBlock = ({ result, channel }: ExchangeResultProps) => {
  const settlement = result.settlement;

  if (isCreditKind(settlement?.kind)) {
    return (
      <div className="flex items-start gap-3 cut-sm bg-success/10 p-4">
        <Gift className="mt-0.5 size-5 shrink-0 text-success" aria-hidden />
        <div className="min-w-0 space-y-1 text-sm">
          <p className="font-semibold text-foreground">
            {channel === "counter"
              ? `Đã cấp tín dụng đổi suất ${formatCurrency(settlement?.amount)} vào tài khoản khách`
              : `Bạn nhận ${formatCurrency(settlement?.amount)} tín dụng đổi suất`}
          </p>
          {settlement?.giftCardCode && (
            <p className="flex flex-wrap items-center gap-2">
              <span className="text-muted-foreground">Mã thẻ</span>
              <span className="t-code font-medium text-foreground">{settlement.giftCardCode}</span>
              <CopyButton value={settlement.giftCardCode} label="mã thẻ" />
            </p>
          )}
          {settlement?.giftCardExpiresAt && (
            <p className="text-muted-foreground">Dùng được đến {formatDate(settlement.giftCardExpiresAt)}</p>
          )}
          <p className="text-xs text-muted-foreground">
            Tín dụng chỉ dùng được cho đúng tài khoản này và không hoàn về cổng thanh toán.
          </p>
        </div>
      </div>
    );
  }

  if (isCashRefundKind(settlement?.kind)) {
    return (
      <div className="flex items-start gap-3 cut-sm bg-success/10 p-4">
        <HandCoins className="mt-0.5 size-5 shrink-0 text-success" aria-hidden />
        <p className="text-sm font-semibold text-foreground">
          Hoàn tiền mặt {formatCurrency(settlement?.amount)} cho khách
        </p>
      </div>
    );
  }

  if (result.amountDue > 0) {
    return (
      <p className="cut-sm bg-success/10 p-4 text-sm font-semibold text-foreground">
        Đã thanh toán thêm {formatCurrency(result.amountDue)}
      </p>
    );
  }

  return null;
};

export const ExchangeResult = ({ result, channel }: ExchangeResultProps): ReactNode => {
  const isCredit = isCreditKind(result.settlement?.kind);
  const newOrderTo =
    channel === "counter"
      ? ROUTES.admin.orderDetail(result.exchangeOrderId)
      : ROUTES.account.ticketDetail(result.exchangeOrderId);
  const originalOrderTo =
    channel === "counter"
      ? ROUTES.admin.orderDetail(result.orderId)
      : ROUTES.account.ticketDetail(result.orderId);

  return (
    <div className="mx-auto flex w-full max-w-lg flex-col gap-4">
      <div className="flex items-start gap-4">
        <CheckCircle2 className="mt-1 size-10 shrink-0 text-success" aria-hidden />
        <div className="space-y-1">
          <h1 className="t-h2 text-foreground">Đổi suất thành công</h1>
          <p className="text-sm text-muted-foreground">
            Vé và mã QR của suất cũ đã vô hiệu. Vé mới đã sẵn sàng
            {channel === "customer" ? " và được gửi qua email." : "."}
          </p>
          <p className="t-code text-sm text-foreground">Mã đơn đổi: {result.exchangeOrderCode}</p>
        </div>
      </div>

      <dl className="grid grid-cols-[1fr_auto] gap-x-4 gap-y-2 cut-md bg-card p-4 text-sm">
        <dt className="text-muted-foreground">Đã trả cho vé cũ</dt>
        <dd className="text-right font-medium tabular-nums text-foreground">{formatCurrency(result.oldAmount)}</dd>
        <dt className="text-muted-foreground">Giá vé suất mới</dt>
        <dd className="text-right font-medium tabular-nums text-foreground">{formatCurrency(result.newAmount)}</dd>
      </dl>

      <SettlementBlock result={result} channel={channel} />

      <div className="flex flex-wrap gap-3">
        <ButtonLink to={newOrderTo}>{channel === "counter" ? "Xem đơn đổi" : "Xem vé mới"}</ButtonLink>
        {isCredit && channel === "customer" && (
          <ButtonLink to={ROUTES.account.giftCards} variant="outline">
            Xem thẻ tín dụng
          </ButtonLink>
        )}
        <ButtonLink to={originalOrderTo} variant="outline">
          Về đơn gốc
        </ButtonLink>
      </div>
    </div>
  );
};
