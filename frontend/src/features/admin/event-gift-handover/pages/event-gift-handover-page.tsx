import { Gift } from "lucide-react";

import { EmptyState } from "@/components/common/empty-state";
import { PageHeader } from "@/components/common/page-header";
import { Card } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";

import { EventGiftLookupPanel } from "../components/event-gift-lookup-panel";
import { EventGiftTicketCard } from "../components/event-gift-ticket-card";
import { useEventGiftHandoverFlow } from "../hooks/use-event-gift-handover-flow";

const AdminEventGiftHandoverPage = () => {
  const flow = useEventGiftHandoverFlow();

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title="Trao quà sự kiện"
        description="Quét QR vé của suất sự kiện để trao quà tặng kèm cho khách."
      />

      <div className="grid grid-cols-1 gap-6 md:grid-cols-5">
        <div className="md:col-span-2">
          <EventGiftLookupPanel flow={flow} />
        </div>

        <div className="md:col-span-3">
          {flow.isLooking && !flow.ticket ? (
            <Card className="gap-3 p-5" aria-busy="true">
              <Skeleton className="h-6 w-40" />
              <Skeleton className="h-16 w-full" />
              <Skeleton className="h-16 w-full" />
            </Card>
          ) : flow.ticket ? (
            <EventGiftTicketCard flow={flow} ticket={flow.ticket} />
          ) : (
            <Card className="p-5">
              <EmptyState
                icon={Gift}
                title="Chưa chọn vé"
                description="Quét QR vé hoặc nhập mã vé để xem quà khách cần nhận."
                className="py-8"
              />
            </Card>
          )}
        </div>
      </div>
    </div>
  );
};

export default AdminEventGiftHandoverPage;
