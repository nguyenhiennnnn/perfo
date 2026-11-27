import { QueryState } from "@/components/common/query-state";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { useMyFreeTickets } from "../hooks/use-membership-plans";
import { FreeTicketCard } from "./free-ticket-card";

interface FreeTicketSectionProps {
  perShowtimeLimit: number;
}

export const FreeTicketSection = ({ perShowtimeLimit }: FreeTicketSectionProps) => {
  const freeTicketsQuery = useMyFreeTickets();

  return (
    <QueryState
      query={freeTicketsQuery}
      skeleton={<CardSkeleton className="h-40" />}
      errorTitle="Không tải được số vé miễn phí"
    >
      {(balance) => <FreeTicketCard balance={balance} perShowtimeLimit={perShowtimeLimit} />}
    </QueryState>
  );
};
