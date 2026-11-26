import { useEffect, useState } from "react";
import { AlertTriangle, FileText } from "lucide-react";
import { useSearchParams } from "react-router";
import { ButtonLink } from "@/components/common/button-link";
import { EmptyState } from "@/components/common/empty-state";
import { PageContainer } from "@/components/common/page-container";
import { Seo } from "@/components/common/seo";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { ROUTES } from "@/config/routes";
import { cn } from "@/lib/utils";
import { RoomRentalPaymentStage, RoomRentalRequestStatus } from "@/types/enum";
import { RoomRentalPaymentSection } from "../components/room-rental-payment-section";
import { RoomRentalQuoteActions } from "../components/room-rental-quote-actions";
import { RoomRentalQuoteCard } from "../components/room-rental-quote-card";
import { RoomRentalQuoteHistory } from "../components/room-rental-quote-history";
import { RoomRentalRequestSummary } from "../components/room-rental-request-summary";
import { RoomRentalTrackingError } from "../components/room-rental-tracking-error";
import { RoomRentalTrackingHeader } from "../components/room-rental-tracking-header";
import {
  ROOM_RENTAL_PAYMENT_PARAM,
  ROOM_RENTAL_PAYMENT_WAIT_MS,
  ROOM_RENTAL_TOKEN_PARAM,
} from "../constants";
import {
  useRoomRentalTracking,
  useRoomRentalTrackingPayments,
  useRoomRentalTrackingQuotes,
} from "../hooks/use-room-rental-tracking";
import { clearRoomRentalPaymentReturn } from "../utils/room-rental-payment-return";
import { hasRoomRentalPayments, hasRoomRentalQuote } from "../utils/room-rental-status";

const PAYMENT_RETURNED = "returned";
const PAYMENT_CANCELED = "canceled";
const STAGE_PARAM = "stage";

const parseStage = (value: string | null): RoomRentalPaymentStage | null =>
  value === RoomRentalPaymentStage.Deposit || value === RoomRentalPaymentStage.Balance
    ? value
    : null;

const TrackingSkeleton = () => (
  <div className="flex flex-col gap-4" aria-busy="true">
    <CardSkeleton />
    <CardSkeleton />
    <CardSkeleton />
  </div>
);

const RoomRentalTrackingPage = () => {
  const [searchParams, setSearchParams] = useSearchParams();
  const token = searchParams.get(ROOM_RENTAL_TOKEN_PARAM)?.trim() ?? "";
  const [paymentResult] = useState(() => searchParams.get(ROOM_RENTAL_PAYMENT_PARAM));
  const [confirmingStage, setConfirmingStage] = useState<RoomRentalPaymentStage | null>(() =>
    searchParams.get(ROOM_RENTAL_PAYMENT_PARAM) === PAYMENT_RETURNED
      ? parseStage(searchParams.get(STAGE_PARAM))
      : null,
  );

  const trackingQuery = useRoomRentalTracking(token, false);
  const { refetch: refetchTracking } = trackingQuery;
  const tracking = trackingQuery.data;
  const status = tracking?.status;

  const quotesQuery = useRoomRentalTrackingQuotes(
    token,
    status !== undefined && hasRoomRentalQuote(status),
  );
  const paymentsQuery = useRoomRentalTrackingPayments(
    token,
    status !== undefined && hasRoomRentalPayments(status),
    confirmingStage !== null,
  );

  useEffect(() => {
    if (!searchParams.has(ROOM_RENTAL_PAYMENT_PARAM)) return;
    clearRoomRentalPaymentReturn();
    setSearchParams(
      (prev) => {
        const next = new URLSearchParams(prev);
        next.delete(ROOM_RENTAL_PAYMENT_PARAM);
        next.delete(STAGE_PARAM);
        return next;
      },
      { replace: true },
    );
  }, [searchParams, setSearchParams]);

  useEffect(() => {
    if (confirmingStage === null) return;
    const timer = window.setTimeout(() => setConfirmingStage(null), ROOM_RENTAL_PAYMENT_WAIT_MS);
    return () => window.clearTimeout(timer);
  }, [confirmingStage]);

  const confirmingPaid = paymentsQuery.data?.stages.find(
    (item) => item.stage === confirmingStage,
  )?.isPaid;
  useEffect(() => {
    if (!confirmingPaid) return;
    setConfirmingStage(null);
    void refetchTracking();
  }, [confirmingPaid, refetchTracking]);

  if (!token) {
    return (
      <PageContainer size="sm" className="py-12">
        <Seo title="Theo dõi yêu cầu thuê phòng" noIndex />
        <EmptyState
          icon={FileText}
          title="Thiếu liên kết theo dõi"
          description="Mở đường dẫn trong email báo giá của rạp để xem yêu cầu thuê phòng của bạn."
          action={<ButtonLink to={ROUTES.roomRental.request}>Gửi yêu cầu thuê phòng</ButtonLink>}
        />
      </PageContainer>
    );
  }

  const versions = quotesQuery.data?.versions ?? [];
  const latest = versions.find((item) => item.isLatest) ?? versions[0];
  const older = versions
    .filter((item) => item.versionNumber !== latest?.versionNumber)
    .sort((a, b) => b.versionNumber - a.versionNumber);
  const canRespond = latest?.canRespond === true && status === RoomRentalRequestStatus.Quoted;

  return (
    <PageContainer
      size="sm"
      className={cn("flex max-w-2xl flex-col gap-4 py-6", canRespond && "pb-36 sm:pb-8")}
    >
      <Seo title="Theo dõi yêu cầu thuê phòng" noIndex />

      <div>
        <h1 className="t-h1 text-foreground">Theo dõi yêu cầu thuê phòng</h1>
        {tracking && (
          <p className="mt-1 text-sm text-muted-foreground">Xin chào {tracking.fullName}.</p>
        )}
      </div>

      {trackingQuery.isLoading && <TrackingSkeleton />}

      {trackingQuery.isError && (
        <RoomRentalTrackingError
          error={trackingQuery.error}
          onRetry={() => void trackingQuery.refetch()}
        />
      )}

      {tracking && (
        <>
          {paymentResult === PAYMENT_CANCELED && (
            <p
              role="alert"
              className="cut-sm flex items-start gap-3 bg-warning/10 px-3 py-3 text-sm text-warning-text"
            >
              <AlertTriangle className="mt-0.5 size-4 shrink-0" aria-hidden />
              <span>Thanh toán chưa hoàn tất. Bạn có thể thử lại ở phần thanh toán bên dưới.</span>
            </p>
          )}

          <RoomRentalTrackingHeader tracking={tracking} />

          {quotesQuery.isLoading && <CardSkeleton />}

          {latest && <RoomRentalQuoteCard quote={latest} token={token} />}

          {latest &&
            !latest.canRespond &&
            status === RoomRentalRequestStatus.Quoted && (
              <p className="text-sm text-muted-foreground" role="status">
                Báo giá này không còn nhận phản hồi. Liên hệ rạp để được hỗ trợ.
              </p>
            )}

          {hasRoomRentalPayments(tracking.status) && (
            <RoomRentalPaymentSection
              query={paymentsQuery}
              token={token}
              confirmingStage={confirmingStage}
              onPaymentSubmitted={setConfirmingStage}
            />
          )}

          <RoomRentalQuoteHistory versions={older} token={token} />

          <RoomRentalRequestSummary tracking={tracking} />

          {canRespond && latest && <RoomRentalQuoteActions quote={latest} token={token} />}
        </>
      )}
    </PageContainer>
  );
};

export default RoomRentalTrackingPage;
