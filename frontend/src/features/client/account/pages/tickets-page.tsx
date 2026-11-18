import { useId, useState } from "react";
import { Ticket as TicketIcon } from "lucide-react";
import { Chip, ChipGroup } from "@/components/common/chip";
import { EmptyState } from "@/components/common/empty-state";
import { PageTitle } from "@/components/common/page-title";
import { QueryState } from "@/components/common/query-state";
import { ButtonLink } from "@/components/common/button-link";
import { SkeletonList } from "@/components/common/skeleton-list";
import { TicketSkeleton } from "@/components/common/skeleton-shapes";
import { InfiniteScrollTrigger } from "@/components/common/infinite-scroll-trigger";
import { TicketCard } from "@/features/client/account/components/ticket-card";
import { useMyBookings, useCancelBooking } from "@/features/client/bookings/hooks/use-bookings";
import { useAlertDialogOpen } from "@/stores/alert-dialog-store";
import { bookingStatusOptions } from "@/lib/enum-options";
import { ACCOUNT_LIST_PAGE_LIMIT } from "@/config/default-constants";
import { ROUTES } from "@/config/routes";

const TAB_ALL_KEY = "all";

const TABS = [{ value: null as string | null, label: "Tất cả" }, ...bookingStatusOptions.map((o) => ({ value: o.value as string | null, label: o.label }))];

const AccountTicketsPage = () => {
  const [status, setStatus] = useState<string | null>(null);
  const tabsId = useId();
  const bookingsQuery = useMyBookings({ status: status ?? undefined, limit: ACCOUNT_LIST_PAGE_LIMIT });
  const { data, hasNextPage, isFetchingNextPage, fetchNextPage } = bookingsQuery;
  const cancelBooking = useCancelBooking();
  const openAlert = useAlertDialogOpen();

  const bookings = data?.bookings ?? [];
  const getTabId = (value: string | null) => `${tabsId}-${value ?? TAB_ALL_KEY}`;

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

      <ChipGroup role="tablist" aria-label="Trạng thái vé">
        {TABS.map((tab) => (
          <Chip
            key={tab.label}
            role="tab"
            id={getTabId(tab.value)}
            selected={status === tab.value}
            onClick={() => setStatus(tab.value)}
          >
            {tab.label}
          </Chip>
        ))}
      </ChipGroup>

      <div role="tabpanel" aria-labelledby={getTabId(status)} className="mt-5 space-y-3">
        <QueryState
          query={bookingsQuery}
          skeleton={<SkeletonList count={3} shape={TicketSkeleton} />}
          isEmpty={bookings.length === 0}
          empty={
            status ? (
              <EmptyState
                variant="filtered"
                title="Không có vé ở trạng thái này"
                description="Chọn trạng thái khác hoặc xem tất cả vé."
                onClearFilters={() => setStatus(null)}
              />
            ) : (
              <EmptyState
                icon={TicketIcon}
                title="Chưa có vé nào"
                description="Đặt vé xem phim để vé và mã QR hiện ở đây."
                action={<ButtonLink to={ROUTES.movies.list} variant="secondary">Xem danh sách phim</ButtonLink>}
              />
            )
          }
          errorTitle="Không tải được danh sách vé"
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