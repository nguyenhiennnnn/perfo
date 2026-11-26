import { useEffect } from "react";
import { ShieldAlert, ShieldCheck } from "lucide-react";

import { Button } from "@/components/ui/button";
import { useCountdown } from "@/hooks/use-countdown";
import { getCustomerRestrictionReasonLabel } from "@/lib/enum-options";
import { formatDateTime } from "@/lib/formatters";
import { useAuthRole, useIsAuthenticated } from "@/stores/auth-store";
import { useCustomerGateStore } from "@/stores/customer-gate-store";
import { useDialogOpen } from "@/stores/dialog-store";
import { Role } from "@/types/enum";
import type { CustomerRestrictionNotice } from "@/lib/error";

import { useOpenVerificationDialog } from "../hooks/use-verification-dialog";
import { useVerificationStatus } from "../hooks/use-verification";
import { formatRemaining } from "../utils/format-remaining";
import { RestrictionDialogContent } from "./restriction-dialog-content";

const BANNER_CLASS =
  "flex flex-col gap-3 bg-warning/10 p-4 sm:flex-row sm:items-center sm:justify-between";

const RestrictionNotice = ({ restriction }: { restriction: CustomerRestrictionNotice }) => {
  const openDialog = useDialogOpen();
  const clearRestriction = useCustomerGateStore((s) => s.clearRestriction);
  const { remainingMs } = useCountdown(restriction.restrictedUntil);

  useEffect(() => {
    if (remainingMs <= 0) clearRestriction();
  }, [remainingMs, clearRestriction]);

  if (remainingMs <= 0) return null;

  return (
    <div role="status" className={BANNER_CLASS}>
      <div className="flex items-start gap-3">
        <ShieldAlert className="mt-0.5 size-5 shrink-0 text-warning-text" aria-hidden />
        <div className="min-w-0">
          <p className="text-sm font-medium text-foreground">
            Tài khoản đang bị hạn chế giữ ghế · {getCustomerRestrictionReasonLabel(restriction.reason)}
          </p>
          <p className="text-xs text-muted-foreground">
            Áp dụng đến {formatDateTime(restriction.restrictedUntil)}, còn{" "}
            <span className="t-code tabular-nums text-foreground">{formatRemaining(remainingMs)}</span>.
          </p>
        </div>
      </div>
      <Button
        variant="outline"
        onClick={() =>
          openDialog({
            title: "Tài khoản đang bị hạn chế giữ ghế",
            content: <RestrictionDialogContent />,
          })
        }
      >
        Xem chi tiết
      </Button>
    </div>
  );
};

const VerificationNotice = () => {
  const openVerificationDialog = useOpenVerificationDialog();

  return (
    <div role="status" className={BANNER_CLASS}>
      <div className="flex items-start gap-3">
        <ShieldCheck className="mt-0.5 size-5 shrink-0 text-warning-text" aria-hidden />
        <div className="min-w-0">
          <p className="text-sm font-medium text-foreground">Cần xác minh để tiếp tục đặt vé</p>
          <p className="text-xs text-muted-foreground">
            Hệ thống phát hiện hoạt động bất thường trên tài khoản. Xác minh bằng mã gửi qua email.
          </p>
        </div>
      </div>
      <Button onClick={openVerificationDialog}>Xác minh ngay</Button>
    </div>
  );
};

export const CustomerGateBanner = () => {
  const isAuthenticated = useIsAuthenticated();
  const role = useAuthRole();
  const isCustomer = isAuthenticated && role === Role.Customer;
  const restriction = useCustomerGateStore((s) => s.restriction);
  const verification = useVerificationStatus(isCustomer);

  if (!isCustomer) return null;

  const showVerification = verification.data?.required === true;
  if (!restriction && !showVerification) return null;

  return (
    <div className="flex flex-col gap-3">
      {restriction && <RestrictionNotice restriction={restriction} />}
      {showVerification && <VerificationNotice />}
    </div>
  );
};
