import { CircleAlert, CircleCheck } from "lucide-react";
import { ButtonLink } from "@/components/common/button-link";
import { CopyButton } from "@/components/common/copy-button";
import { SubmitButton } from "@/components/common/form/submit-button";
import { Button } from "@/components/ui/button";
import { ROUTES } from "@/config/routes";
import { useBlockDialogClose } from "@/hooks/use-block-dialog-close";
import { formatDateTime, formatPoints } from "@/lib/formatters";
import { useDialogClose } from "@/stores/dialog-store";
import { useRedeemLoyaltyReward } from "../hooks/use-loyalty";
import type { LoyaltyRewardCatalogItemDto } from "../types";
import {
  CODE_NO_REFUND_NOTICE,
  describeCodeValidity,
  describeRewardBenefit,
} from "../utils/loyalty-labels";

interface LoyaltyRedeemDialogContentProps {
  reward: LoyaltyRewardCatalogItemDto;
  balance: number;
}

export const LoyaltyRedeemDialogContent = ({
  reward,
  balance,
}: LoyaltyRedeemDialogContentProps) => {
  const close = useDialogClose();
  const redeem = useRedeemLoyaltyReward();
  useBlockDialogClose(redeem.isPending);

  const result = redeem.data?.data;

  if (result) {
    return (
      <div className="space-y-4">
        <p className="flex items-center gap-2 text-sm font-medium text-success">
          <CircleCheck className="size-5 shrink-0" aria-hidden />
          Đổi quà thành công
        </p>

        <div className="space-y-1 bg-secondary p-3">
          <p className="text-xs text-muted-foreground">Mã khuyến mãi của bạn</p>
          <div className="flex items-center justify-between gap-2">
            <span data-testid="loyalty-redeem-code" className="t-code text-base text-foreground">
              {result.code}
            </span>
            <CopyButton value={result.code} label="mã quà" />
          </div>
          <p className="text-xs text-muted-foreground">
            Hạn dùng mã: {formatDateTime(result.codeExpiresAt)}
          </p>
        </div>

        <p className="flex items-start gap-2 text-sm text-muted-foreground">
          <CircleAlert className="mt-0.5 size-4 shrink-0 text-warning-text" aria-hidden />
          <span>{result.notice}</span>
        </p>

        <dl className="space-y-1 text-sm">
          <div className="flex justify-between gap-3">
            <dt className="text-muted-foreground">Điểm đã dùng</dt>
            <dd className="font-medium tabular-nums">{formatPoints(result.pointsSpent)}</dd>
          </div>
          <div className="flex justify-between gap-3">
            <dt className="text-muted-foreground">Số dư còn lại</dt>
            <dd className="font-medium tabular-nums">{formatPoints(result.balanceAfter)}</dd>
          </div>
        </dl>

        <div className="flex justify-end gap-2">
          <Button variant="outline" onClick={close}>
            Đóng
          </Button>
          <ButtonLink to={ROUTES.schedule} onClick={close}>
            Đặt vé ngay
          </ButtonLink>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <div>
        <p className="font-medium text-foreground">{reward.name}</p>
        <p className="text-sm text-muted-foreground">{describeRewardBenefit(reward)}</p>
      </div>

      <dl className="space-y-1 text-sm">
        <div className="flex justify-between gap-3">
          <dt className="text-muted-foreground">Số dư hiện tại</dt>
          <dd className="font-medium tabular-nums">{formatPoints(balance)}</dd>
        </div>
        <div className="flex justify-between gap-3">
          <dt className="text-muted-foreground">Điểm đổi quà</dt>
          <dd className="font-medium tabular-nums">-{formatPoints(reward.pointsCost)}</dd>
        </div>
        <div className="flex justify-between gap-3 border-t border-border pt-1">
          <dt className="text-muted-foreground">Số dư sau khi đổi</dt>
          <dd className="font-semibold tabular-nums text-accent-text">
            {formatPoints(balance - reward.pointsCost)}
          </dd>
        </div>
      </dl>

      <p role="note" className="flex items-start gap-2 bg-warning/10 p-3 text-sm text-warning-text">
        <CircleAlert className="mt-0.5 size-4 shrink-0" aria-hidden />
        <span>
          {describeCodeValidity(reward.codeValidityDays)}. {CODE_NO_REFUND_NOTICE}
        </span>
      </p>

      <div className="flex justify-end gap-2">
        <Button variant="outline" disabled={redeem.isPending} onClick={close}>
          Hủy
        </Button>
        <SubmitButton
          isPending={redeem.isPending}
          label="Xác nhận đổi"
          pendingLabel="Đang đổi..."
          disabled={balance < reward.pointsCost}
          onClick={() => redeem.mutate(reward.rewardId)}
        />
      </div>
    </div>
  );
};
