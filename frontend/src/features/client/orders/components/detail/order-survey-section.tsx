import { ExperienceSurveyCard } from "@/features/client/surveys/components/experience-survey-card";
import { OrderStatus } from "@/types/enum";
import type { OrderDetailDto } from "../../types";
import { getActiveTicketLines } from "../../utils/order-lines";

interface OrderSurveySectionProps {
  order: OrderDetailDto;
}

export const OrderSurveySection = ({ order }: OrderSurveySectionProps) => {
  if (order.status !== OrderStatus.Confirmed) return null;

  const showtimeIds = [
    ...new Set(
      getActiveTicketLines(order)
        .filter((line) => line.ticket.isCheckedIn)
        .map((line) => line.ticket.showtimeId),
    ),
  ];
  if (showtimeIds.length === 0) return null;

  return (
    <div className="space-y-4">
      {showtimeIds.map((showtimeId) => (
        <ExperienceSurveyCard key={showtimeId} showtimeId={showtimeId} />
      ))}
    </div>
  );
};
