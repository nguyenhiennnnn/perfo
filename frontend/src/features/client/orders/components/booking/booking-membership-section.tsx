import { useState } from "react";
import { AlertTriangle, CircleAlert, Crown } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { useActiveMembership } from "@/features/client/memberships/hooks/use-active-membership";
import {
  useApplyFreeTicket,
  useMyFreeTickets,
  useRemoveFreeTicket,
} from "@/features/client/memberships/hooks/use-membership-plans";
import { describePlanDiscounts } from "@/features/client/memberships/utils/membership-plans";
import { useCinemaSettings } from "@/features/client/settings/hooks/use-settings";
import { getDisplayMessage, isFreeTicketShowtimeLimitError } from "@/lib/error";
import { formatCurrency } from "@/lib/formatters";
import { cn } from "@/lib/utils";
import type { OrderDetailDto } from "../../types";
import { getFreeTicketCandidates, getMembershipBenefitAmount } from "../../utils/order-membership";

interface BookingMembershipSectionProps {
  order: OrderDetailDto;
  disabled?: boolean;
  onFullyCovered: () => void;
}

interface Notice {
  kind: "limit" | "error";
  message: string;
}

export const BookingMembershipSection = ({
  order,
  disabled,
  onFullyCovered,
}: BookingMembershipSectionProps) => {
  const { subscription, plan, isLoading } = useActiveMembership();
  const balanceQuery = useMyFreeTickets(subscription !== null);
  const perShowtimeLimit = useCinemaSettings().data?.membershipFreeTicketsPerShowtime ?? 0;
  const applyFreeTicket = useApplyFreeTicket(order.orderId);
  const removeFreeTicket = useRemoveFreeTicket(order.orderId);
  const [pendingLineId, setPendingLineId] = useState<number | null>(null);
  const [notice, setNotice] = useState<Notice | null>(null);

  const benefitAmount = getMembershipBenefitAmount(order);
  const candidates = getFreeTicketCandidates(order);
  const appliedCount = candidates.filter((candidate) => candidate.isApplied).length;
  const remaining = balanceQuery.data?.remaining ?? 0;

  if (isLoading) return <Skeleton className="h-36 w-full" />;
  if (!subscription && benefitAmount === 0 && appliedCount === 0) return null;

  const planName = plan?.name ?? subscription?.planName ?? "thành viên";
  const discountText = plan ? describePlanDiscounts(plan) : null;
  const showFreeTickets = (plan?.freeTicketsPerMonth ?? 0) > 0 || appliedCount > 0 || remaining > 0;
  const limitReachedOnOrder = perShowtimeLimit > 0 && appliedCount >= perShowtimeLimit;
  const isBusy = pendingLineId !== null;

  const run = async (orderLineId: number, action: () => Promise<{ data: { isFullyCovered: boolean } }>) => {
    setNotice(null);
    setPendingLineId(orderLineId);
    try {
      const response = await action();
      if (response.data.isFullyCovered) onFullyCovered();
    } catch (error) {
      setNotice({
        kind: isFreeTicketShowtimeLimitError(error) ? "limit" : "error",
        message: getDisplayMessage(error, { preferServerMessage: true }),
      });
    } finally {
      setPendingLineId(null);
    }
  };

  return (
    <section aria-label="Quyền lợi gói thành viên" className="flex flex-col gap-3 bg-card p-4">
      <div className="flex items-center justify-between gap-3">
        <h3 className="t-h3 flex items-center gap-2 text-foreground">
          <Crown className="size-4 text-accent-text" aria-hidden />
          Quyền lợi gói thành viên
        </h3>
        <Badge variant="secondary">{planName}</Badge>
      </div>

      {benefitAmount > 0 ? (
        <div className="flex items-start justify-between gap-3 bg-secondary p-3">
          <div className="min-w-0">
            <p className="text-sm font-medium text-foreground">Giảm từ gói {planName}</p>
            {discountText && <p className="text-xs text-muted-foreground">Gói {discountText}</p>}
          </div>
          <span
            data-testid="membership-benefit-discount"
            className="shrink-0 text-sm font-semibold tabular-nums text-success"
          >
            -{formatCurrency(benefitAmount)}
          </span>
        </div>
      ) : (
        discountText && (
          <p className="text-xs text-muted-foreground">
            Gói {discountText}. Chưa có khoản giảm từ gói trên đơn này.
          </p>
        )
      )}

      {benefitAmount > 0 && plan && !plan.stackableWithPromotions && (
        <p className="text-xs text-muted-foreground">Gói này không dùng chung với mã khuyến mãi.</p>
      )}

      {showFreeTickets && (
        <div className="flex flex-col gap-2">
          <div className="flex items-center justify-between gap-3">
            <p className="text-sm font-medium text-foreground">Vé miễn phí</p>
            <p className="text-xs text-muted-foreground">
              Còn{" "}
              <span data-testid="membership-free-ticket-remaining" className="font-semibold tabular-nums text-foreground">
                {remaining}
              </span>{" "}
              vé trong kỳ này
            </p>
          </div>

          <ul className="flex flex-col gap-2">
            {candidates.map(({ line, isApplied, isEligible }) => {
              const value = line.lockedUnitPrice * line.quantity;
              const isPending = pendingLineId === line.orderLineId;
              const blocked = !isApplied && (!isEligible || remaining <= 0 || limitReachedOnOrder);
              return (
                <li
                  key={line.orderLineId}
                  data-testid="membership-free-ticket-line"
                  className={cn(
                    "flex items-center justify-between gap-3 border border-border p-3",
                    isApplied && "border-success/40 bg-success/5",
                  )}
                >
                  <div className="flex min-w-0 flex-col gap-0.5">
                    <span className="text-sm font-medium text-foreground">
                      Ghế {line.ticket.seatRow}
                      {line.ticket.seatNumber}
                      {line.description ? ` · ${line.description}` : ""}
                    </span>
                    <span className="flex items-center gap-2 text-xs tabular-nums text-muted-foreground">
                      <span className={cn(isApplied && "line-through")}>{formatCurrency(value)}</span>
                      {isApplied && <Badge variant="success">Miễn phí</Badge>}
                      {!isEligible && !isApplied && <span>Không dùng được vé miễn phí</span>}
                    </span>
                  </div>
                  {isApplied ? (
                    <Button
                      type="button"
                      variant="outline"
                      size="sm"
                      disabled={disabled || isBusy}
                      aria-label={`Gỡ vé miễn phí ghế ${line.ticket.seatRow}${line.ticket.seatNumber}`}
                      onClick={() =>
                        void run(line.orderLineId, () => removeFreeTicket.mutateAsync(line.orderLineId))
                      }
                    >
                      {isPending ? "Đang gỡ..." : "Gỡ"}
                    </Button>
                  ) : (
                    <Button
                      type="button"
                      size="sm"
                      disabled={disabled || isBusy || blocked}
                      aria-label={`Dùng vé miễn phí cho ghế ${line.ticket.seatRow}${line.ticket.seatNumber}`}
                      onClick={() =>
                        void run(line.orderLineId, () => applyFreeTicket.mutateAsync(line.orderLineId))
                      }
                    >
                      {isPending ? "Đang áp..." : "Dùng vé miễn phí"}
                    </Button>
                  )}
                </li>
              );
            })}
          </ul>

          {notice?.kind === "limit" && (
            <p
              role="alert"
              data-testid="membership-free-ticket-limit-notice"
              className="flex items-start gap-2 bg-warning/10 p-3 text-sm text-foreground"
            >
              <AlertTriangle className="mt-0.5 size-4 shrink-0 text-warning" aria-hidden />
              <span className="flex flex-col gap-0.5">
                <span className="font-medium">Không thể dùng thêm vé miễn phí cho suất này</span>
                <span className="text-xs text-muted-foreground">
                  {notice.message} Các vé còn lại sẽ thanh toán theo giá của đơn.
                </span>
              </span>
            </p>
          )}
          {notice?.kind === "error" && (
            <p role="alert" className="flex items-start gap-1.5 text-sm text-destructive">
              <CircleAlert className="mt-0.5 size-4 shrink-0" aria-hidden />
              <span>{notice.message}</span>
            </p>
          )}

          {notice === null && limitReachedOnOrder && (
            <p className="text-xs text-muted-foreground">
              Đã dùng đủ {perShowtimeLimit} vé miễn phí cho suất chiếu này.
            </p>
          )}
          {remaining <= 0 && appliedCount === 0 && (
            <p className="text-xs text-muted-foreground">
              Bạn không còn vé miễn phí khả dụng trong kỳ này.
            </p>
          )}
          {perShowtimeLimit > 0 && !limitReachedOnOrder && (
            <p className="text-xs text-muted-foreground">
              Mỗi suất chiếu dùng tối đa {perShowtimeLimit} vé miễn phí.
            </p>
          )}
          {disabled && (
            <p className="text-xs text-muted-foreground">
              Gỡ thẻ quà tặng trước khi thay đổi vé miễn phí.
            </p>
          )}
        </div>
      )}
    </section>
  );
};
