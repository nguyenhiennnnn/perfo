import { TriangleAlert } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { paymentStatusMap } from "@/components/common/status-badge.config";
import { FAILED_LABEL } from "@/config/default-constants";
import { PaymentStatus } from "@/types/enum";

import { getTransactionTypeLabel, isRefundTransaction } from "../utils/transaction-labels";

const CHARGE_STATUS_LABELS: Record<string, string> = {
  [PaymentStatus.Pending]: "Chờ thanh toán",
  [PaymentStatus.Success]: "Đã thu",
  [PaymentStatus.Failed]: FAILED_LABEL,
};

const REFUND_STATUS_LABELS: Record<string, string> = {
  [PaymentStatus.Pending]: "Chờ hoàn",
  [PaymentStatus.Success]: "Đã hoàn",
  [PaymentStatus.Failed]: "Hoàn thất bại",
};

export const TransactionTypeBadge = ({ type }: { type: string }) => (
  <Badge variant={isRefundTransaction(type) ? "warning" : "outline"}>
    {getTransactionTypeLabel(type)}
  </Badge>
);

export const TransactionStatusBadge = ({ type, status }: { type: string; status: string }) => {
  const item = paymentStatusMap[status as PaymentStatus];
  const labels = isRefundTransaction(type) ? REFUND_STATUS_LABELS : CHARGE_STATUS_LABELS;
  const Icon = item?.icon;

  return (
    <Badge variant={item?.variant ?? "outline"}>
      {Icon && <Icon className="size-3" />}
      {labels[status] ?? status}
    </Badge>
  );
};

export const CashRefundFlagBadge = () => (
  <Badge variant="destructive">
    <TriangleAlert />
    Cần hoàn tiền mặt
  </Badge>
);
