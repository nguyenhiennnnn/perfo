import { Ticket } from "lucide-react";
import { ButtonLink } from "@/components/common/button-link";
import { ROUTES } from "@/config/routes";
import { formatDate } from "@/lib/formatters";
import type { MembershipFreeTicketBalanceDto } from "../types";

interface FreeTicketCardProps {
  balance: MembershipFreeTicketBalanceDto;
  perShowtimeLimit: number;
}

export const FreeTicketCard = ({ balance, perShowtimeLimit }: FreeTicketCardProps) => (
  <section aria-label="Vé miễn phí còn lại" className="cut-md bg-card p-5">
    <div className="flex flex-wrap items-start justify-between gap-4">
      <div>
        <p className="text-xs text-muted-foreground">Vé miễn phí còn lại trong kỳ này</p>
        <p className="t-numeral mt-1 text-4xl text-accent-text">{balance.remaining}</p>
      </div>
      {balance.remaining > 0 && (
        <ButtonLink to={ROUTES.schedule}>
          <Ticket className="size-4" aria-hidden />
          Đặt vé ngay
        </ButtonLink>
      )}
    </div>

    {balance.periods.length > 0 ? (
      <ul className="mt-4 flex flex-col gap-3">
        {balance.periods.map((period) => (
          <li
            key={`${period.membershipSubscriptionId}-${period.periodStart}`}
            className="flex flex-wrap items-center justify-between gap-2 bg-muted/50 px-3 py-2 text-sm"
          >
            <span className="text-foreground">
              {period.planName}: đã dùng{" "}
              <span className="font-semibold tabular-nums">
                {period.used}/{period.granted}
              </span>{" "}
              vé
            </span>
            <span className="text-xs text-muted-foreground">
              Còn <span className="font-semibold tabular-nums text-foreground">{period.remaining}</span>{" "}
              vé, dùng trước {formatDate(period.periodEnd)}
            </span>
          </li>
        ))}
      </ul>
    ) : (
      <p className="mt-4 text-sm text-muted-foreground">
        Chưa có vé miễn phí trong kỳ này. Vé được cấp vào đầu mỗi tháng hiệu lực của gói.
      </p>
    )}

    <p className="mt-4 border-t border-border pt-3 text-xs text-muted-foreground">
      Vé miễn phí chỉ dùng trong tháng được cấp và không cộng dồn sang tháng sau.
      {perShowtimeLimit > 0 &&
        ` Mỗi suất chiếu dùng tối đa ${perShowtimeLimit} vé miễn phí.`}
    </p>
  </section>
);
