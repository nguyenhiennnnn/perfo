import { Navigate, useSearchParams } from "react-router";
import { ROUTES } from "@/config/routes";
import BookingResultPage from "@/features/client/orders/pages/booking-result-page";
import { readRoomRentalPaymentReturn } from "../utils/room-rental-payment-return";

const BookingResultGatePage = () => {
  const [searchParams] = useSearchParams();
  const orderId = Number(searchParams.get("bookingId"));
  const rentalReturn = orderId ? readRoomRentalPaymentReturn(orderId) : null;

  if (rentalReturn) {
    return (
      <Navigate
        replace
        to={ROUTES.roomRental.track(rentalReturn.token, {
          payment: searchParams.get("canceled") === "true" ? "canceled" : "returned",
          stage: rentalReturn.stage,
        })}
      />
    );
  }

  return <BookingResultPage />;
};

export default BookingResultGatePage;
