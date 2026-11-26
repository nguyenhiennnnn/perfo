import type { UseQueryResult } from "@tanstack/react-query";
import { QueryState } from "@/components/common/query-state";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { formatCurrency } from "@/lib/formatters";
import { RoomRentalPaymentStage, RoomRentalRequestStatus } from "@/types/enum";
import type { RoomRentalPaymentsDto } from "../types";
import { RoomRentalPaymentStageCard } from "./room-rental-payment-stage-card";

interface RoomRentalPaymentSectionProps {
  query: UseQueryResult<RoomRentalPaymentsDto>;
  token: string;
  confirmingStage: RoomRentalPaymentStage | null;
  onPaymentSubmitted: (stage: RoomRentalPaymentStage) => void;
}

const isStageActionable = (
  stage: RoomRentalPaymentStage,
  status: RoomRentalRequestStatus,
): boolean =>
  stage === RoomRentalPaymentStage.Deposit
    ? status === RoomRentalRequestStatus.Accepted
    : status === RoomRentalRequestStatus.DepositPaid;

export const RoomRentalPaymentSection = ({
  query,
  token,
  confirmingStage,
  onPaymentSubmitted,
}: RoomRentalPaymentSectionProps) => (
  <section className="cut-md flex flex-col gap-4 bg-card p-4" aria-label="Thanh toán">
    <h2 className="t-h3 text-foreground">Thanh toán</h2>
    <QueryState
      query={query}
      skeleton={
        <div className="flex flex-col gap-3" aria-busy="true">
          <CardSkeleton />
          <CardSkeleton />
        </div>
      }
      errorTitle="Không tải được thông tin thanh toán"
    >
      {(payments) => (
        <>
          <dl className="grid grid-cols-2 gap-3 bg-secondary p-3 text-sm">
            <div>
              <dt className="text-xs text-muted-foreground">Tổng giá trị</dt>
              <dd className="tabular font-semibold text-foreground">
                {formatCurrency(payments.total)}
              </dd>
            </div>
            <div>
              <dt className="text-xs text-muted-foreground">Đã thanh toán</dt>
              <dd className="tabular font-semibold text-success">
                {formatCurrency(payments.paidTotal)}
              </dd>
            </div>
          </dl>
          <div className="flex flex-col gap-3">
            {payments.stages.map((stage) => (
              <RoomRentalPaymentStageCard
                key={stage.stage}
                stage={stage}
                token={token}
                isActionable={isStageActionable(stage.stage, payments.status)}
                isConfirming={confirmingStage === stage.stage}
                onPaymentSubmitted={onPaymentSubmitted}
              />
            ))}
          </div>
        </>
      )}
    </QueryState>
  </section>
);
