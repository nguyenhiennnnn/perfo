import { useEffect, useState } from "react";
import { CheckCircle2 } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Chip, ChipGroup } from "@/components/common/chip";
import { FormErrorAlert } from "@/components/common/form/form-error-alert";
import { FORM_PROCESSING_LABEL } from "@/config/default-constants";
import { formatCurrency, formatDate } from "@/lib/formatters";
import { cn } from "@/lib/utils";
import { useDialogClose, useDialogDismissGuard } from "@/stores/dialog-store";
import { MembershipRefundReason } from "@/types/enum";
import {
  getPaymentMethodLabel,
  getRefundStatusInfo,
} from "@/features/admin/orders/utils/order-cancel";

import { useAdminRefundMembershipSubscription } from "../hooks/use-admin-membership-plans";
import type { AdminMembershipSubscriptionDto, MembershipRefundResultDto } from "../types";
import {
  MEMBERSHIP_REFUND_NOTE_MAX_LENGTH,
  REFUND_REASON_OPTIONS,
} from "../utils/membership-plan-admin-labels";

const REFUND_SUMMARY: Record<MembershipRefundResultDto["refundStatus"], string> = {
  None: "Không phát sinh khoản hoàn tiền.",
  Refunded: "Các khoản hoàn đã được thực hiện xong.",
  Pending: "Còn khoản hoàn đang chờ xử lý, xem chi tiết bên dưới.",
};

interface MembershipRefundDialogContentProps {
  subscription: AdminMembershipSubscriptionDto;
}

export const MembershipRefundDialogContent = ({
  subscription,
}: MembershipRefundDialogContentProps) => {
  const closeDialog = useDialogClose();
  const { block, release } = useDialogDismissGuard();
  const refund = useAdminRefundMembershipSubscription();

  const [reason, setReason] = useState<MembershipRefundReason | null>(null);
  const [note, setNote] = useState("");
  const [result, setResult] = useState<MembershipRefundResultDto | null>(null);

  useEffect(() => {
    if (!refund.isPending) return;
    block();
    return release;
  }, [refund.isPending, block, release]);

  const trimmedNote = note.trim();
  const noteTooLong = note.length > MEMBERSHIP_REFUND_NOTE_MAX_LENGTH;
  const canSubmit =
    reason !== null && trimmedNote.length > 0 && !noteTooLong && !refund.isPending;
  const selectedReason = REFUND_REASON_OPTIONS.find((option) => option.value === reason);

  const handleSubmit = async (event: React.SyntheticEvent) => {
    event.preventDefault();
    if (!canSubmit || reason === null) return;
    try {
      const response = await refund.mutateAsync({
        id: subscription.membershipSubscriptionId,
        body: { reason, note: trimmedNote },
      });
      setResult(response.data);
    } catch {
      return;
    }
  };

  if (result) {
    return (
      <section className="flex flex-col gap-4">
        <div className="flex items-start gap-3">
          <CheckCircle2 className="mt-0.5 size-5 shrink-0 text-success" />
          <div className="min-w-0">
            <p className="font-heading text-base font-semibold text-foreground">
              Đã hoàn gói của {subscription.customerName}
            </p>
            <p className="text-sm text-muted-foreground">{REFUND_SUMMARY[result.refundStatus]}</p>
          </div>
        </div>

        <dl className="divide-y divide-border cut-sm bg-secondary px-4 text-sm">
          <div className="flex items-center justify-between gap-3 py-3">
            <dt className="text-muted-foreground">Đã hoàn cho khách</dt>
            <dd className="font-semibold tabular-nums text-foreground">
              {formatCurrency(result.totalRefunded)}
            </dd>
          </div>
          <div className="flex items-center justify-between gap-3 py-3">
            <dt className="text-muted-foreground">Doanh thu dừng ghi nhận</dt>
            <dd className="tabular-nums text-foreground">
              {formatCurrency(result.stoppedRevenueAmount)} ({result.stoppedMonths} tháng)
            </dd>
          </div>
          <div className="flex items-center justify-between gap-3 py-3">
            <dt className="text-muted-foreground">Vé miễn phí bị thu hồi</dt>
            <dd className="tabular-nums text-foreground">{result.revokedFreeTickets}</dd>
          </div>
          <div className="flex items-center justify-between gap-3 py-3">
            <dt className="text-muted-foreground">Đơn mua gói</dt>
            <dd className="text-foreground">
              <span className="t-code">{result.orderCode}</span>
              {result.orderCancelled ? " · đã hủy" : ""}
            </dd>
          </div>
          {result.refunds.map((item) => {
            const status = getRefundStatusInfo(item.status);
            return (
              <div
                key={`${item.method}-${item.status}`}
                className="flex items-center justify-between gap-3 py-3"
              >
                <div className="flex min-w-0 flex-wrap items-center gap-2">
                  <dt className="font-medium text-foreground">
                    {getPaymentMethodLabel(item.method)}
                  </dt>
                  <Badge variant={status.variant}>{status.label}</Badge>
                </div>
                <dd className="shrink-0 font-semibold tabular-nums text-foreground">
                  {formatCurrency(item.amount)}
                </dd>
              </div>
            );
          })}
        </dl>

        <Button type="button" onClick={closeDialog}>
          Đóng
        </Button>
      </section>
    );
  }

  return (
    <form onSubmit={(e) => void handleSubmit(e)} className="flex flex-col gap-4">
      <div className="flex flex-col gap-1 cut-sm bg-muted px-3 py-2 text-sm">
        <p className="text-foreground">
          <span className="font-medium">{subscription.customerName}</span> · {subscription.planName}{" "}
          · {formatCurrency(subscription.planPrice)}
        </p>
        <p className="text-muted-foreground">
          Hiệu lực {formatDate(subscription.validFrom)} - {formatDate(subscription.validTo)} · đơn{" "}
          <span className="t-code">{subscription.orderCode}</span>
        </p>
      </div>

      <ul className="list-disc space-y-1 pl-5 text-sm text-muted-foreground">
        <li>Hoàn lại phần tiền tương ứng các tháng chưa ghi nhận doanh thu.</li>
        <li>Dừng ghi nhận doanh thu các tháng còn lại và thu hồi vé miễn phí chưa dùng.</li>
        <li>Quyền lợi của gói mất hiệu lực ngay. Thao tác được ghi vào nhật ký và không thể hoàn tác.</li>
      </ul>

      <div className="flex flex-col gap-2">
        <p id="membership-refund-reason-label" className="text-sm font-medium text-foreground">
          Lý do hoàn <span className="text-destructive">*</span>
        </p>
        <ChipGroup role="radiogroup" aria-labelledby="membership-refund-reason-label">
          {REFUND_REASON_OPTIONS.map((option) => (
            <Chip
              key={option.value}
              role="radio"
              selected={reason === option.value}
              disabled={refund.isPending}
              onClick={() => setReason(option.value)}
            >
              {option.label}
            </Chip>
          ))}
        </ChipGroup>
        <p className="text-xs text-muted-foreground">
          {selectedReason?.description ?? "Chọn lý do để ghi vào nhật ký thao tác."}
        </p>
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="membership-refund-note">Ghi chú (bắt buộc)</Label>
        <Textarea
          id="membership-refund-note"
          rows={3}
          className="min-h-20 resize-none"
          value={note}
          aria-invalid={noteTooLong || undefined}
          disabled={refund.isPending}
          onChange={(e) => setNote(e.target.value)}
        />
        <p className={cn("text-xs", noteTooLong ? "text-destructive" : "text-muted-foreground")}>
          {note.length}/{MEMBERSHIP_REFUND_NOTE_MAX_LENGTH}
        </p>
      </div>

      <FormErrorAlert error={refund.error} preferServerMessage />

      <div className="grid grid-cols-2 gap-2">
        <Button type="button" variant="outline" onClick={closeDialog} disabled={refund.isPending}>
          Quay lại
        </Button>
        <Button type="submit" variant="destructive" disabled={!canSubmit}>
          {refund.isPending ? FORM_PROCESSING_LABEL : "Hoàn gói"}
        </Button>
      </div>
    </form>
  );
};
