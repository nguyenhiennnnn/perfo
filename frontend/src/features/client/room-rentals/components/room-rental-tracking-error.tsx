import { useState } from "react";
import { Clock, LinkIcon } from "lucide-react";
import { ButtonLink } from "@/components/common/button-link";
import { EmptyState } from "@/components/common/empty-state";
import { ErrorState } from "@/components/common/error-state";
import { MS_PER_SECOND } from "@/config/default-constants";
import { ROUTES } from "@/config/routes";
import { useCountdown } from "@/hooks/use-countdown";
import { getPublicFormRetrySeconds, isNotFoundError } from "@/lib/error";

interface RoomRentalTrackingErrorProps {
  error: unknown;
  onRetry: () => void;
}

const LockedNotice = ({ seconds }: { seconds: number }) => {
  const [lockedUntil] = useState(() => new Date(Date.now() + seconds * MS_PER_SECOND).toISOString());
  const countdown = useCountdown(lockedUntil);

  return (
    <div
      role="alert"
      className="cut-sm flex items-start gap-3 bg-destructive/10 px-3 py-3 text-sm text-destructive"
    >
      <Clock className="mt-0.5 size-4 shrink-0" aria-hidden />
      <div className="space-y-0.5">
        <p className="font-medium">Liên kết tạm thời bị khóa</p>
        <p>
          Có quá nhiều lần thử không hợp lệ. Vui lòng thử lại sau{" "}
          <span className="tabular font-semibold">{countdown.label}</span>.
        </p>
      </div>
    </div>
  );
};

export const RoomRentalTrackingError = ({ error, onRetry }: RoomRentalTrackingErrorProps) => {
  const retrySeconds = getPublicFormRetrySeconds(error);
  if (retrySeconds !== null) return <LockedNotice seconds={retrySeconds || 60} />;

  if (isNotFoundError(error)) {
    return (
      <EmptyState
        icon={LinkIcon}
        title="Liên kết không đúng hoặc đã hết hạn"
        description="Kiểm tra lại đường dẫn trong email báo giá, hoặc liên hệ rạp để được gửi lại liên kết."
        action={<ButtonLink to={ROUTES.roomRental.request}>Gửi yêu cầu thuê phòng</ButtonLink>}
      />
    );
  }

  return <ErrorState title="Không tải được yêu cầu thuê phòng" error={error} onRetry={onRetry} />;
};
