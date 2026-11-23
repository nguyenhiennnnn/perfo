import { Button } from "@/components/ui/button";
import { ComboPicker } from "@/components/common/combo-picker/combo-picker";
import { useActiveCombos } from "@/features/client/combos/hooks/use-combos";

import type { CounterFlow } from "../hooks/use-counter-flow";
import { SeatTicketList } from "@/components/common/seat-ticket-list";
import { CounterSeatPanel } from "./counter-seat-panel";
import { CounterCustomerForm } from "./counter-customer-form";
import { CounterPriceSummary } from "./counter-price-summary";
import { HOLDING_SEATS_LABEL } from "@/config/default-constants";

interface CounterSeatsStepProps {
  flow: CounterFlow;
}

export function CounterSeatsStep({ flow }: CounterSeatsStepProps) {
  const activeCombos = useActiveCombos();

  return (
    <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
      <div className="lg:col-span-2">
        <CounterSeatPanel
          query={flow.showtimeSeats}
          statusMap={flow.seatStatusMap}
          selectedIds={flow.selectedSeatIds}
          onToggleSeat={flow.toggleSeat}
        />
      </div>

      <div className="flex flex-col gap-4">
        <SeatTicketList
          audience="counter"
          seats={flow.seatTicketTypes.pricedSeats}
          ticketTypes={flow.seatTicketTypes.ticketTypes}
          onChange={flow.seatTicketTypes.setTicketType}
          disabled={flow.isHolding}
        />

        <div className="bg-card p-4">
          <h3 className="t-h3 mb-3 text-foreground">Combo</h3>
          <ComboPicker
            combos={activeCombos.data}
            isLoading={activeCombos.isLoading}
            isError={activeCombos.isError}
            error={activeCombos.error}
            onRetry={() => void activeCombos.refetch()}
            quantities={flow.comboQuantities}
            onChange={flow.setComboQuantity}
          />
        </div>

        <div className="bg-card p-4">
          <h3 className="t-h3 mb-3 text-foreground">Thông tin khách</h3>
          <CounterCustomerForm
            customerName={flow.customerName}
            onCustomerNameChange={flow.setCustomerName}
            customerPhone={flow.customerPhone}
            onCustomerPhoneChange={flow.setCustomerPhone}
            userId={flow.userId}
            onUserIdChange={flow.setUserId}
          />
        </div>

        <CounterPriceSummary
          seats={flow.selectedSeatsInfo}
          combos={activeCombos.data}
          comboQuantities={flow.comboQuantities}
        />

        <Button disabled={!flow.canHold || flow.isHolding} onClick={flow.holdSeats}>
          {flow.isHolding ? HOLDING_SEATS_LABEL : "Giữ ghế & tiếp tục"}
        </Button>
      </div>
    </div>
  );
}
