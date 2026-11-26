import { Button } from "@/components/ui/button";
import { ButtonLink } from "@/components/common/button-link";
import { ROUTES } from "@/config/routes";
import { useCountdown } from "@/hooks/use-countdown";
import { getCustomerRestrictionReasonLabel } from "@/lib/enum-options";
import { formatDateTime } from "@/lib/formatters";
import { useCustomerGateStore } from "@/stores/customer-gate-store";
import { useDialogClose } from "@/stores/dialog-store";

import { formatRemaining } from "../utils/format-remaining";

export const RestrictionDialogContent = () => {
  const restriction = useCustomerGateStore((s) => s.restriction);
  const close = useDialogClose();
  const { remainingMs } = useCountdown(restriction?.restrictedUntil ?? null);

  if (!restriction) return null;

  return (
    <div className="flex flex-col gap-4">
      {restriction.message && <p className="text-sm text-foreground">{restriction.message}</p>}

      <dl className="grid grid-cols-[auto_1fr] gap-x-4 gap-y-2 text-sm">
        <dt className="text-muted-foreground">Lý do</dt>
        <dd className="font-medium text-foreground">
          {getCustomerRestrictionReasonLabel(restriction.reason)}
        </dd>
        <dt className="text-muted-foreground">Áp dụng đến</dt>
        <dd className="t-code tabular-nums text-foreground">
          {formatDateTime(restriction.restrictedUntil)}
        </dd>
        <dt className="text-muted-foreground">Còn lại</dt>
        <dd className="t-code tabular-nums text-foreground">{formatRemaining(remainingMs)}</dd>
      </dl>

      <p className="text-xs text-muted-foreground">
        Trong thời gian này, thời gian giữ ghế được rút ngắn và số đơn chờ thanh toán cùng lúc bị
        giới hạn. Hãy thanh toán hoặc hủy đơn đang chờ để đặt thêm.
      </p>

      <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
        <ButtonLink to={ROUTES.account.tickets} variant="outline" onClick={close}>
          Xem vé của tôi
        </ButtonLink>
        <Button onClick={close}>Đã hiểu</Button>
      </div>
    </div>
  );
};
