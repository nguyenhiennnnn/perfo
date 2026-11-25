import { useMemo, useState, type ReactNode } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { Ticket } from "lucide-react";
import { useNavigate } from "react-router";
import { queryKeys } from "@/api/query-keys";
import { ButtonLink } from "@/components/common/button-link";
import { EmptyState } from "@/components/common/empty-state";
import { usePriceChangeConfirm } from "@/hooks/use-price-change-confirm";
import { getPriceChangeRequired } from "@/lib/error";
import { OrderStatus } from "@/types/enum";
import type { ShowtimeSeatItemDto } from "@/features/client/showtimes/types";
import { useExchangePreview, useStartExchange } from "../../hooks/use-order-exchange";
import type {
  OrderDetailDto,
  OrderExchangeAdapter,
  StartOrderExchangeRequest,
  StartOrderExchangeResponse,
} from "../../types";
import { getExchangeableLines, type ExchangePairing } from "../../utils/order-exchange";
import { ExchangeResult } from "./exchange-result";
import { ExchangeSeatStep } from "./exchange-seat-step";
import { ExchangeShowtimeStep } from "./exchange-showtime-step";
import { ExchangeStepper, type ExchangeStep } from "./exchange-stepper";

export interface ExchangePaymentContext {
  exchange: StartOrderExchangeResponse;
  order: OrderDetailDto;
  onCompleted: () => void;
  onBackToSeats: () => void;
  onExit: () => void;
}

interface OrderExchangeFlowProps {
  order: OrderDetailDto;
  adapter: OrderExchangeAdapter;
  backTo: string;
  getPaymentRoute?: (exchange: StartOrderExchangeResponse) => string;
  renderPayment?: (context: ExchangePaymentContext) => ReactNode;
  unavailableNotice?: ReactNode;
}

export const OrderExchangeFlow = ({
  order,
  adapter,
  backTo,
  getPaymentRoute,
  renderPayment,
  unavailableNotice,
}: OrderExchangeFlowProps) => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const confirmPriceChange = usePriceChangeConfirm();
  const startExchange = useStartExchange(adapter);

  const exchangeableLines = useMemo(() => getExchangeableLines(order), [order]);
  const [step, setStep] = useState<ExchangeStep>("showtime");
  const [selectedLineIds, setSelectedLineIds] = useState<number[]>(() =>
    exchangeableLines.map((line) => line.orderLineId),
  );
  const [showtimeId, setShowtimeId] = useState<number | null>(null);
  const [started, setStarted] = useState<StartOrderExchangeResponse | null>(null);

  const preview = useExchangePreview(adapter, order.orderId, selectedLineIds, step === "showtime");
  const selectedLines = exchangeableLines.filter((line) => selectedLineIds.includes(line.orderLineId));

  const toggleLine = (lineId: number, checked: boolean) => {
    setShowtimeId(null);
    setSelectedLineIds((prev) =>
      checked ? [...prev.filter((id) => id !== lineId), lineId] : prev.filter((id) => id !== lineId),
    );
  };

  const handleStarted = (response: StartOrderExchangeResponse) => {
    setStarted(response);
    if (response.status === OrderStatus.Confirmed) {
      setStep("done");
      return;
    }
    if (getPaymentRoute) {
      navigate(getPaymentRoute(response));
      return;
    }
    setStep("payment");
  };

  const handleConfirm = async (pairings: ExchangePairing[], seats: ShowtimeSeatItemDto[]) => {
    if (showtimeId == null) return;
    const buildBody = (acceptPriceChange: boolean): StartOrderExchangeRequest => ({
      newShowtimeId: showtimeId,
      seats: pairings.map((pairing) => ({
        orderLineId: pairing.line.orderLineId,
        newShowtimeSeatId: pairing.seat.showtimeSeatId,
        expectedPrice: pairing.newPrice / pairing.line.quantity,
      })),
      acceptPriceChange,
    });
    const submit = (acceptPriceChange: boolean) =>
      startExchange.mutateAsync({ orderId: order.orderId, body: buildBody(acceptPriceChange) });
    const refreshSeats = () =>
      void queryClient.invalidateQueries({ queryKey: queryKeys.showtimes.seats(showtimeId) });

    try {
      handleStarted((await submit(false)).data);
    } catch (error) {
      const change = getPriceChangeRequired(error);
      if (!change) {
        refreshSeats();
        return;
      }
      const previousTotal = pairings.reduce((sum, pairing) => sum + pairing.newPrice, 0);
      const accepted = await confirmPriceChange(change, seats, previousTotal);
      if (!accepted) {
        refreshSeats();
        return;
      }
      try {
        handleStarted((await submit(true)).data);
      } catch {
        refreshSeats();
      }
    }
  };

  if (step === "done" && started) {
    return <ExchangeResult result={started} channel={adapter.channel} />;
  }

  if (step === "payment" && started && renderPayment) {
    return (
      <>
        {renderPayment({
          exchange: started,
          order,
          onCompleted: () => setStep("done"),
          onBackToSeats: () => {
            setStarted(null);
            setStep("seats");
          },
          onExit: () => navigate(backTo),
        })}
      </>
    );
  }

  if (exchangeableLines.length === 0) {
    return (
      unavailableNotice ?? (
        <EmptyState
          icon={Ticket}
          title="Đơn này không còn vé nào đổi được"
          description="Vé đã check-in, đã hoàn, đã đổi hoặc suất chiếu đã bắt đầu thì không đổi được."
          action={<ButtonLink to={backTo}>Quay lại đơn</ButtonLink>}
        />
      )
    );
  }

  return (
    <div className="flex flex-col gap-6">
      <ExchangeStepper step={step} />
      {step === "showtime" && (
        <ExchangeShowtimeStep
          lines={exchangeableLines}
          selectedLineIds={selectedLineIds}
          onToggleLine={toggleLine}
          previewQuery={preview}
          selectedShowtimeId={showtimeId}
          onSelectShowtime={setShowtimeId}
          onContinue={() => setStep("seats")}
        />
      )}
      {step === "seats" && showtimeId != null && (
        <ExchangeSeatStep
          channel={adapter.channel}
          showtimeId={showtimeId}
          lines={selectedLines}
          isSubmitting={startExchange.isPending}
          onBack={() => setStep("showtime")}
          onConfirm={(pairings, seats) => void handleConfirm(pairings, seats)}
        />
      )}
    </div>
  );
};
