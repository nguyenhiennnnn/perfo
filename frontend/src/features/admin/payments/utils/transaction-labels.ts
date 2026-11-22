import { FAILED_LABEL } from "@/config/default-constants";
import type { EnumOption } from "@/lib/enum-options";
import { PaymentMethod, PaymentStatus, PaymentTransactionType } from "@/types/enum";

import type { AdminPaymentListItemDto } from "../types/ledger";

export const transactionTypeOptions: EnumOption[] = [
  { value: PaymentTransactionType.Charge, label: "Thu tiền" },
  { value: PaymentTransactionType.Refund, label: "Hoàn tiền" },
];

export const transactionStatusOptions: EnumOption[] = [
  { value: PaymentStatus.Pending, label: "Chờ xử lý" },
  { value: PaymentStatus.Success, label: "Thành công" },
  { value: PaymentStatus.Failed, label: FAILED_LABEL },
];

export const getTransactionTypeLabel = (type: string) =>
  transactionTypeOptions.find((option) => option.value === type)?.label ?? type;

export const isRefundTransaction = (type: string) => type === PaymentTransactionType.Refund;

export const needsCashRefund = (
  item: Pick<AdminPaymentListItemDto, "needsManualRefund" | "type" | "method" | "status">,
) =>
  item.needsManualRefund ||
  (isRefundTransaction(item.type) &&
    item.method === PaymentMethod.Cash &&
    item.status === PaymentStatus.Pending);
