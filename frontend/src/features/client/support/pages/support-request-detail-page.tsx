import { useParams } from "react-router";
import { ArrowLeft, LifeBuoy } from "lucide-react";
import { ButtonLink } from "@/components/common/button-link";
import { NotFoundState } from "@/components/common/not-found-state";
import { PageContainer } from "@/components/common/page-container";
import { QueryState } from "@/components/common/query-state";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { ROUTES } from "@/config/routes";
import { SupportRequestStatus } from "@/types/enum";
import { SupportMessageThread } from "../components/support-message-thread";
import { SupportReplyForm } from "../components/support-reply-form";
import { SupportRequestHeader } from "../components/support-request-header";
import { useSupportRequestDetail } from "../hooks/use-support-requests";
import type { SupportRequestDto } from "../types";

const SupportRequestDetailSkeleton = () => (
  <div className="space-y-4">
    <CardSkeleton className="h-40" />
    <CardSkeleton className="h-24" />
    <CardSkeleton className="h-24" />
  </div>
);

const SupportRequestDetailContent = ({ request }: { request: SupportRequestDto }) => {
  const isResolved = request.status === SupportRequestStatus.Resolved;

  return (
    <div className="space-y-6">
      <SupportRequestHeader request={request} />

      <SupportMessageThread messages={request.messages} />

      {isResolved ? (
        <div className="space-y-3 cut-md bg-card p-4" data-testid="support-request-resolved">
          <p className="text-sm text-muted-foreground">
            Yêu cầu này đã được giải quyết nên không thể trả lời thêm. Nếu vẫn cần hỗ trợ, hãy gửi yêu cầu mới
            từ chi tiết đơn hàng.
          </p>
          <ButtonLink to={ROUTES.account.ticketDetail(request.orderId)} variant="outline">
            Mở đơn {request.orderCode}
          </ButtonLink>
        </div>
      ) : (
        <SupportReplyForm requestId={request.supportRequestId} />
      )}
    </div>
  );
};

const AccountSupportRequestDetailPage = () => {
  const { id } = useParams<{ id: string }>();
  const requestQuery = useSupportRequestDetail(Number(id));

  return (
    <PageContainer size="sm" className="flex flex-col gap-4">
      <div>
        <ButtonLink to={ROUTES.account.supportRequests} variant="ghost" size="sm">
          <ArrowLeft className="size-4" />
          Yêu cầu hỗ trợ
        </ButtonLink>
      </div>
      <QueryState
        query={requestQuery}
        skeleton={<SupportRequestDetailSkeleton />}
        notFound={
          <NotFoundState
            entity="yêu cầu hỗ trợ"
            icon={LifeBuoy}
            backLabel="Xem yêu cầu hỗ trợ"
            backTo={ROUTES.account.supportRequests}
          />
        }
        errorTitle="Không tải được yêu cầu hỗ trợ"
      >
        {(request) => <SupportRequestDetailContent request={request} />}
      </QueryState>
    </PageContainer>
  );
};

export default AccountSupportRequestDetailPage;
