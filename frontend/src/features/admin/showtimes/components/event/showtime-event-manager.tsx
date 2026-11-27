import { useState } from "react";

import { QueryState } from "@/components/common/query-state";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { Button } from "@/components/ui/button";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { isNotFoundError } from "@/lib/error";
import { useAlertDialogOpen } from "@/stores/alert-dialog-store";

import {
  useRemoveShowtimeEvent,
  useShowtimeEvent,
  useShowtimeEventEarlyAccess,
  useShowtimeEventGifts,
  useShowtimeEventLimits,
} from "../../hooks/use-showtime-event";
import { ShowtimeEventEarlyAccessForm } from "./showtime-event-early-access-form";
import { ShowtimeEventGiftsForm } from "./showtime-event-gifts-form";
import { ShowtimeEventLimitsForm } from "./showtime-event-limits-form";
import { ShowtimeEventPricingForm } from "./showtime-event-pricing-form";

interface ShowtimeEventManagerProps {
  showtimeId: number;
  onClose: () => void;
}

type EventTab = "pricing" | "limits" | "early-access" | "gifts";

export const ShowtimeEventManager = ({ showtimeId, onClose }: ShowtimeEventManagerProps) => {
  const [tab, setTab] = useState<EventTab>("pricing");
  const eventQuery = useShowtimeEvent(showtimeId);
  const removeEvent = useRemoveShowtimeEvent();
  const openAlert = useAlertDialogOpen();

  const isEvent = eventQuery.isSuccess;
  const limits = useShowtimeEventLimits(showtimeId, isEvent);
  const gifts = useShowtimeEventGifts(showtimeId, isEvent);
  const earlyAccess = useShowtimeEventEarlyAccess(showtimeId, isEvent);

  const handleRemove = () =>
    openAlert({
      title: "Gỡ suất sự kiện?",
      description:
        "Suất chiếu trở lại thành suất thường; toàn bộ cấu hình phụ thu, hạn mức, mở bán sớm và quà tặng kèm bị xóa. Chỉ gỡ được khi suất chưa có đơn.",
      confirmText: "Gỡ sự kiện",
      variant: "destructive",
      onConfirm: async () => {
        await removeEvent.mutateAsync(showtimeId);
        onClose();
      },
    });

  const notAnEvent = eventQuery.isError && isNotFoundError(eventQuery.error);

  return (
    <QueryState
      query={
        notAnEvent
          ? { ...eventQuery, isError: false, data: undefined, isLoading: false }
          : eventQuery
      }
      skeleton={<CardSkeleton className="h-64" />}
      errorTitle="Không tải được cấu hình sự kiện"
    >
      <Tabs value={tab} onValueChange={(value) => setTab(value as EventTab)}>
        <TabsList variant="line" className="flex-nowrap overflow-x-auto">
          <TabsTrigger value="pricing">Giá và phụ thu</TabsTrigger>
          <TabsTrigger value="limits" disabled={!isEvent}>
            Hạn mức vé
          </TabsTrigger>
          <TabsTrigger value="early-access" disabled={!isEvent}>
            Mở bán sớm
          </TabsTrigger>
          <TabsTrigger value="gifts" disabled={!isEvent}>
            Quà tặng kèm
          </TabsTrigger>
        </TabsList>

        <TabsContent value="pricing" className="pt-4">
          <ShowtimeEventPricingForm
            key={JSON.stringify(eventQuery.data ?? null)}
            showtimeId={showtimeId}
            event={eventQuery.data}
            onSaved={() => setTab("limits")}
          />
          {isEvent && (
            <div className="mt-4 flex justify-start border-t border-border pt-4">
              <Button
                type="button"
                variant="destructive"
                onClick={handleRemove}
                disabled={removeEvent.isPending}
              >
                Gỡ suất sự kiện
              </Button>
            </div>
          )}
        </TabsContent>

        <TabsContent value="limits" className="pt-4">
          <QueryState query={limits} skeleton={<CardSkeleton className="h-48" />}>
            {(data) => (
              <ShowtimeEventLimitsForm
                key={JSON.stringify(data)}
                showtimeId={showtimeId}
                limits={data}
              />
            )}
          </QueryState>
        </TabsContent>

        <TabsContent value="early-access" className="pt-4">
          <QueryState query={earlyAccess} skeleton={<CardSkeleton className="h-48" />}>
            {(data) => (
              <ShowtimeEventEarlyAccessForm
                key={JSON.stringify(data)}
                showtimeId={showtimeId}
                setting={data}
              />
            )}
          </QueryState>
        </TabsContent>

        <TabsContent value="gifts" className="pt-4">
          <QueryState query={gifts} skeleton={<CardSkeleton className="h-48" />}>
            {(data) => (
              <ShowtimeEventGiftsForm
                key={JSON.stringify(data)}
                showtimeId={showtimeId}
                gifts={data}
              />
            )}
          </QueryState>
        </TabsContent>
      </Tabs>
    </QueryState>
  );
};
