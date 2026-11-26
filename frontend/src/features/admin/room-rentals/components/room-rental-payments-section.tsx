import { Banknote } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { LineSkeleton } from "@/components/common/skeleton-shapes";
import { formatCurrency, formatDateTime } from "@/lib/formatters";
import { useDialogClose, useDialogOpen } from "@/stores/dialog-store";
import { RoomRentalPaymentStage, RoomRentalRequestStatus } from "@/types/enum";
import { useAdminRoomRentalPayments } from "../hooks/use-admin-room-rental-payments";
import {
  ROOM_RENTAL_PAYMENT_ORDER_STATUS,
  ROOM_RENTAL_PAYMENT_STAGE_LABELS,
} from "../utils/room-rental-admin-labels";
import { RoomRentalBankTransferDialogContent } from "./room-rental-bank-transfer-dialog-content";
import type { RoomRentalPaymentStageDto } from "../types";

interface RoomRentalPaymentsSectionProps {
  requestId: number;
  requestCode: string;
  status: RoomRentalRequestStatus;
}

const canRecord = (stage: RoomRentalPaymentStageDto, status: RoomRentalRequestStatus) =>
  !stage.isPaid &&
  stage.amount > 0 &&
  ((stage.stage === RoomRentalPaymentStage.Deposit && status === RoomRentalRequestStatus.Accepted) ||
    (stage.stage === RoomRentalPaymentStage.Balance && status === RoomRentalRequestStatus.DepositPaid));

export const RoomRentalPaymentsSection = ({
  requestId,
  requestCode,
  status,
}: RoomRentalPaymentsSectionProps) => {
  const payments = useAdminRoomRentalPayments(requestId, status);
  const openDialog = useDialogOpen();
  const closeDialog = useDialogClose();

  if (payments.isLoading) {
    return (
      <section className="space-y-3 cut-md bg-card p-4">
        <LineSkeleton className="h-5 w-40" />
        <LineSkeleton className="h-16 w-full" />
        <LineSkeleton className="h-16 w-full" />
      </section>
    );
  }
  if (!payments.data) return null;

  const { data } = payments;
  const openRecord = (stage: RoomRentalPaymentStageDto) =>
    openDialog({
      title: `Ghi nhận chuyển khoản · ${ROOM_RENTAL_PAYMENT_STAGE_LABELS[stage.stage]}`,
      description: `Yêu cầu ${requestCode}`,
      content: (
        <RoomRentalBankTransferDialogContent
          requestId={requestId}
          stage={stage}
          onDone={closeDialog}
        />
      ),
    });

  return (
    <section className="space-y-3 cut-md bg-card p-4">
      <div className="flex flex-wrap items-baseline justify-between gap-2">
        <h2 className="t-h3 text-foreground">Thanh toán</h2>
        <p className="text-sm text-muted-foreground">
          Đã thu <strong className="tabular-nums text-foreground">{formatCurrency(data.paidTotal)}</strong> /{" "}
          <span className="tabular-nums">{formatCurrency(data.total)}</span>
        </p>
      </div>
      <ul className="divide-y divide-border">
        {data.stages.map((stage) => {
          const orderStatus = stage.order
            ? (ROOM_RENTAL_PAYMENT_ORDER_STATUS[stage.order.status] ?? {
                label: stage.order.status,
                variant: "secondary" as const,
              })
            : null;
          return (
            <li key={stage.stage} className="flex flex-wrap items-center justify-between gap-3 py-3">
              <div className="min-w-0 space-y-1">
                <p className="flex flex-wrap items-center gap-2 text-sm font-medium text-foreground">
                  {ROOM_RENTAL_PAYMENT_STAGE_LABELS[stage.stage]}
                  {stage.isPaid ? (
                    <Badge variant="success">Đã thu đủ</Badge>
                  ) : orderStatus ? (
                    <Badge variant={orderStatus.variant}>{orderStatus.label}</Badge>
                  ) : (
                    <Badge variant="secondary">Chưa tạo đơn</Badge>
                  )}
                </p>
                <p className="text-sm text-muted-foreground">
                  <span className="tabular-nums">{formatCurrency(stage.amount)}</span>
                  {stage.dueAt && <> · hạn {formatDateTime(stage.dueAt)}</>}
                  {stage.order && (
                    <>
                      {" "}
                      · đơn <span className="t-code">{stage.order.orderCode}</span>
                    </>
                  )}
                </p>
                {stage.order && !stage.isPaid && stage.order.paidAmount > 0 && (
                  <p className="text-xs text-muted-foreground">
                    Đã nhận {formatCurrency(stage.order.paidAmount)}, còn thiếu{" "}
                    {formatCurrency(stage.order.outstandingAmount)}
                  </p>
                )}
              </div>
              {canRecord(stage, status) && (
                <Button size="sm" variant="outline" onClick={() => openRecord(stage)}>
                  <Banknote className="size-4" />
                  <span>Ghi nhận chuyển khoản</span>
                </Button>
              )}
            </li>
          );
        })}
      </ul>
    </section>
  );
};
