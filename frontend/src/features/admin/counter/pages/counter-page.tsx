import { useState } from "react";
import { PageHeader } from "@/components/common/page-header";
import { Button } from "@/components/ui/button";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";

import { useCounterFlow } from "../hooks/use-counter-flow";
import { useCounterComboSale } from "../hooks/use-counter-combo-sale";
import { CounterShowtimePicker } from "../components/counter-showtime-picker";
import { CounterSeatsStep } from "../components/counter-seats-step";
import { CounterCheckoutStep } from "../components/counter-checkout-step";
import { CounterTicketResult } from "../components/counter-ticket-result";
import { CounterComboSale } from "../components/counter-combo-sale";
import { CounterCancelTab } from "../components/counter-cancel-tab";
import { CounterShiftBar } from "../components/counter-shift-bar";
import { CounterShiftGate } from "../components/counter-shift-gate";
import type { CounterShift } from "../types/counter-shift";

type CounterTab = "tickets" | "combo" | "cancel";

interface CounterSalesProps {
  shift: CounterShift;
}

const CounterSales = ({ shift }: CounterSalesProps) => {
  const [tab, setTab] = useState<CounterTab>("tickets");
  const flow = useCounterFlow();
  const comboSale = useCounterComboSale();

  const ticketBusy = flow.isHolding || flow.isPaying || flow.isCancelling;
  const ticketLocked = flow.step === "checkout" || ticketBusy;
  const comboLocked = comboSale.hasPendingOrder;

  const showTicketReset = tab === "tickets" && flow.step !== "showtime";
  const showComboReset = tab === "combo" && comboSale.isDirty;
  const shiftCloseLocked = ticketLocked || comboLocked;

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title="Bán hàng tại quầy"
        description="Bán vé theo suất chiếu, bán riêng combo bắp nước hoặc hủy đơn hàng cho khách."
        action={
          showTicketReset ? (
            <Button variant="outline" onClick={flow.resetFlow} disabled={ticketBusy}>
              Bắt đầu lại
            </Button>
          ) : showComboReset ? (
            <Button
              variant="outline"
              onClick={() => void comboSale.reset()}
              disabled={comboSale.isPaying || comboSale.isReleasing}
            >
              Bắt đầu lại
            </Button>
          ) : undefined
        }
      />

      <CounterShiftBar shift={shift} closeDisabled={shiftCloseLocked} />

      <Tabs value={tab} onValueChange={(value) => setTab(value as CounterTab)}>
        <TabsList>
          <TabsTrigger value="tickets" disabled={comboLocked}>
            Bán vé
          </TabsTrigger>
          <TabsTrigger value="combo" disabled={ticketLocked}>
            Bán combo
          </TabsTrigger>
          <TabsTrigger value="cancel" disabled={ticketLocked || comboLocked}>
            Hủy đơn
          </TabsTrigger>
        </TabsList>

        <TabsContent value="tickets" className="flex flex-col gap-6">
          {flow.step === "showtime" && (
            <CounterShowtimePicker
              date={flow.selectedDate}
              onDateChange={flow.setSelectedDate}
              onPickShowtime={flow.pickShowtime}
            />
          )}

          {flow.step === "seats" && <CounterSeatsStep flow={flow} />}

          {flow.step === "checkout" && <CounterCheckoutStep flow={flow} />}

          {flow.step === "done" && (
            <CounterTicketResult
              orderQuery={flow.orderDetail}
              payments={flow.payments}
              totalChange={flow.totalChange}
              onNewSale={flow.resetFlow}
            />
          )}
        </TabsContent>

        <TabsContent value="combo">
          <CounterComboSale sale={comboSale} />
        </TabsContent>

        <TabsContent value="cancel">
          <CounterCancelTab />
        </TabsContent>
      </Tabs>
    </div>
  );
};

const AdminCounterPage = () => (
  <CounterShiftGate>{(shift) => <CounterSales shift={shift} />}</CounterShiftGate>
);

export default AdminCounterPage;
