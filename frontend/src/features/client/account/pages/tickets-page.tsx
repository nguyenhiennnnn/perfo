import { useState } from "react";
import { Ticket as TicketIcon } from "lucide-react";
import { EmptyState } from "@/components/common/empty-state";
import { PageTitle } from "@/components/common/page-title";
import { QueryState } from "@/components/common/query-state";
import { SkeletonList } from "@/components/common/skeleton-list";
import { InfiniteScrollTrigger } from "@/components/common/infinite-scroll-trigger";
import { TicketCard } from "@/features/client/account/components/ticket-card";
import { useMyBookings, useCancelBooking } from "@/features/client/bookings/hooks/use-bookings";
import { useAlertDialogOpen } from "@/stores/alert-dialog-store";
import { bookingStatusOptions } from "@/lib/enum-options";
import { cn } from "@/lib/utils";
import { ACCOUNT_LIST_PAGE_LIMIT } from "@/config/default-constants";

const TABS = [{ value: null as string | null, label: "Tất cả" }, ...bookingStatusOptions.map((o) => ({ value: o.value as string | null, label: o.label }))];

const AccountTicketsPage = () => {
  const [status, setStatus] = useState<string | null>(null);
  const bookingsQuery = useMyBookings({ status: status ?? undefined, limit: ACCOUNT_LIST_PAGE_LIMIT });
  const { data, hasNextPage, isFetchingNextPage, fetchNextPage } = bookingsQuery;
  const cancelBooking = useCancelBooking();
  const openAlert = useAlertDialogOpen();

  const bookings = data?.bookings ?? [];

  const handleCancel = (bookingId: number) => {
    openAlert({
      title: "Hủy vé & hoàn tiền",
      description: "Bạn có chắc muốn hủy vé này? Số tiền sẽ được hoàn theo chính sách của rạp.",
      confirmText: "Hủy vé",
      variant: "destructive",
      onConfirm: () => cancelBooking.mutate(bookingId),
    });
  };

  return (
    <div>
      <PageTitle title="Vé của tôi" />

      <div className="flex flex-wrap gap-2">
        {TABS.map((tab) => (
          <button
            key={tab.label}
            type="button"
            onClick={() => setStatus(tab.value)}
            className={cn(
              "border px-3.5 py-1.5 text-sm font-medium transition-colors",
              status === tab.value
                ? "border-primary bg-primary text-primary-foreground"
                : "border-border bg-secondary text-secondary-foreground hover:border-primary/50 hover:bg-muted",
            )}
          >
            {tab.label}
          </button>
        ))}
      </div>

      <div className="mt-5 space-y-3">
        <QueryState
          query={bookingsQuery}
          skeleton={<SkeletonList count={3} className="h-24 w-full" />}
          isEmpty={bookings.length === 0}
          empty={<EmptyState icon={TicketIcon} title="Chưa có vé nào" description="Hãy đặt vé để bắt đầu trải nghiệm điện ảnh." />}
          errorTitle="Không tải được danh sách vé"
          errorDescription="Kiểm tra kết nối mạng rồi thử lại."
        >
          {bookings.map((booking) => (
            <TicketCard key={booking.bookingId} booking={booking} onCancel={() => handleCancel(booking.bookingId)} />
          ))}
          <InfiniteScrollTrigger hasNextPage={hasNextPage} isFetchingNextPage={isFetchingNextPage} fetchNextPage={fetchNextPage} />
        </QueryState>
      </div>
    </div>
  );
}

export default AccountTicketsPage;