import { Clock } from "lucide-react";

interface RoomRentalRateLimitAlertProps {
  countdownLabel: string;
}

export const RoomRentalRateLimitAlert = ({ countdownLabel }: RoomRentalRateLimitAlertProps) => (
  <div
    role="alert"
    className="cut-sm flex items-start gap-3 bg-destructive/10 px-3 py-3 text-sm text-destructive"
  >
    <Clock className="mt-0.5 size-4 shrink-0" />
    <div className="space-y-0.5">
      <p className="font-medium">Bạn gửi yêu cầu quá nhiều lần</p>
      <p>
        Vui lòng thử lại sau <span className="tabular font-semibold">{countdownLabel}</span>.
        Thông tin bạn đã nhập vẫn được giữ lại.
      </p>
    </div>
  </div>
);
