import { useParams } from "react-router";
import { ArrowLeft } from "lucide-react";
import { ButtonLink } from "@/components/common/button-link";
import { NotFoundState } from "@/components/common/not-found-state";
import { QueryState } from "@/components/common/query-state";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { Button } from "@/components/ui/button";
import { ROUTES } from "@/config/routes";
import { formatDateTime } from "@/lib/formatters";
import { RoomRentalRequestStatus } from "@/types/enum";
import { useDialogClose, useDialogOpen } from "@/stores/dialog-store";
import { useSheetClose, useSheetOpen } from "@/stores/sheet-store";
import { RoomRentalQuoteDetail } from "../components/room-rental-quote-detail";
import { RoomRentalQuoteVersions } from "../components/room-rental-quote-versions";
import { RoomRentalQuoteForm } from "../components/forms/room-rental-quote-form";
import { RoomRentalRequestInfo } from "../components/room-rental-request-info";
import { RoomRentalStatusBadge } from "../components/room-rental-status-badge";
import { RoomRentalStatusDialogContent } from "../components/room-rental-status-dialog-content";
import { roomRentalsAdminApi } from "../api/room-rentals-admin-api";
import { useAdminRoomRental } from "../hooks/use-admin-room-rentals";
import {
  QUOTABLE_STATUSES,
  ROOM_RENTAL_ADMIN_STATUS,
  ROOM_RENTAL_STATUS_ACTION_LABELS,
  getManualNextStatuses,
} from "../utils/room-rental-admin-labels";
import type { RoomRentalAdminDto, RoomRentalQuoteVersionDto } from "../types";

const DetailSkeleton = () => (
  <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_minmax(0,1fr)]">
    <CardSkeleton className="h-96" />
    <CardSkeleton className="h-96" />
  </div>
);

const RoomRentalRequestContent = ({ request }: { request: RoomRentalAdminDto }) => {
  const openSheet = useSheetOpen();
  const closeSheet = useSheetClose();
  const openDialog = useDialogOpen();
  const closeDialog = useDialogClose();
  const requestId = request.roomRentalRequestId;
  const canQuote = QUOTABLE_STATUSES.includes(request.status);
  const nextStatuses = getManualNextStatuses(request.status, request.allowedNextStatuses);

  const openComposer = (sourceVersion?: number) => {
    const open = (source: RoomRentalQuoteVersionDto | null) =>
      openSheet({
        title: source ? "Sửa báo giá thành phiên bản mới" : "Soạn báo giá",
        description: `Yêu cầu ${request.requestCode} · ${request.fullName}`,
        size: "xl",
        content: <RoomRentalQuoteForm request={request} source={source} onDone={closeSheet} />,
      });

    if (sourceVersion === undefined) {
      open(null);
      return;
    }
    void roomRentalsAdminApi.getQuote(requestId, sourceVersion).then((res) => open(res.data));
  };

  const openView = (version: number) =>
    openDialog({
      title: `Báo giá phiên bản ${version}`,
      description: `Yêu cầu ${request.requestCode}`,
      content: <RoomRentalQuoteDetail requestId={requestId} version={version} />,
    });

  const openStatusChange = (target: (typeof nextStatuses)[number]) =>
    openDialog({
      title: ROOM_RENTAL_STATUS_ACTION_LABELS[target] ?? `Chuyển sang "${ROOM_RENTAL_ADMIN_STATUS[target].label}"`,
      content: (
        <RoomRentalStatusDialogContent requestId={requestId} target={target} onDone={closeDialog} />
      ),
    });

  return (
    <div className="space-y-6">
      <div className="space-y-2">
        <ButtonLink to={ROUTES.admin.roomRentals} variant="ghost" size="sm" className="-ml-2">
          <ArrowLeft className="size-4" />
          <span>Danh sách yêu cầu</span>
        </ButtonLink>
        <div className="flex flex-wrap items-center gap-2">
          <h1 className="t-h2 text-foreground">Yêu cầu {request.requestCode}</h1>
          <RoomRentalStatusBadge status={request.status} />
        </div>
        <p className="text-sm text-muted-foreground">
          {request.fullName} · gửi {formatDateTime(request.createdAt)} · cập nhật {formatDateTime(request.updatedAt)}
        </p>
        {nextStatuses.length > 0 && (
          <div className="flex flex-wrap gap-2 pt-1">
            {nextStatuses.map((status) => (
              <Button
                key={status}
                size="sm"
                variant={status === RoomRentalRequestStatus.Cancelled ? "destructive" : "outline"}
                onClick={() => openStatusChange(status)}
              >
                {ROOM_RENTAL_STATUS_ACTION_LABELS[status] ?? ROOM_RENTAL_ADMIN_STATUS[status].label}
              </Button>
            ))}
          </div>
        )}
      </div>

      <div className="grid items-start gap-6 lg:grid-cols-[minmax(0,1fr)_minmax(0,1fr)]">
        <RoomRentalRequestInfo request={request} />
        <RoomRentalQuoteVersions
          requestId={requestId}
          canQuote={canQuote}
          contactEmail={request.contactEmail}
          onCompose={() => openComposer()}
          onReviseFrom={(version) => openComposer(version)}
          onView={openView}
        />
      </div>
    </div>
  );
};

const AdminRoomRentalRequestDetailPage = () => {
  const { id } = useParams();
  const requestId = Number(id);
  const request = useAdminRoomRental(Number.isInteger(requestId) ? requestId : 0);

  return (
    <QueryState
      query={request}
      skeleton={<DetailSkeleton />}
      notFound={<NotFoundState entity="yêu cầu thuê phòng" />}
    >
      {(data) => <RoomRentalRequestContent request={data} />}
    </QueryState>
  );
};

export default AdminRoomRentalRequestDetailPage;
