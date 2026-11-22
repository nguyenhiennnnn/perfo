import { TriangleAlert } from "lucide-react";
import { HTTP_STATUS } from "@/config/http-status";
import { extractApiError, getDisplayMessage } from "@/lib/error";

const getErrorTitle = (error: unknown) => {
  const { statusCode } = extractApiError(error);
  if (statusCode === HTTP_STATUS.conflict)
    return "Đơn đang được xử lý hoặc đã bị hủy";
  if (statusCode === HTTP_STATUS.notFound) return "Không tìm thấy đơn hàng";
  return "Không thể hủy đơn";
};

export const OrderCancelErrorNotice = ({ error }: { error: unknown }) => (
  <div
    role="alert"
    data-testid="counter-cancel-error"
    className="flex items-start gap-3 cut-sm bg-destructive/10 p-3 text-destructive"
  >
    <TriangleAlert className="mt-0.5 size-4 shrink-0" />
    <div className="min-w-0">
      <p className="text-sm font-semibold">{getErrorTitle(error)}</p>
      <p className="mt-0.5 text-sm wrap-break-word">
        {getDisplayMessage(error, { preferServerMessage: true })}
      </p>
    </div>
  </div>
);
