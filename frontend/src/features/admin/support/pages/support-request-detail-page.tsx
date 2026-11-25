import type { ReactNode } from "react";
import { Link, useParams } from "react-router";
import { ArrowLeft, Gift, HandCoins, LifeBuoy, MailCheck } from "lucide-react";
import { ButtonLink } from "@/components/common/button-link";
import { NotFoundState } from "@/components/common/not-found-state";
import { QueryState } from "@/components/common/query-state";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { StaffSupportRequestStatusBadge } from "@/components/common/status-badge";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { ROUTES } from "@/config/routes";
import { formatDateTime } from "@/lib/formatters";
import { useAlertDialogOpen } from "@/stores/alert-dialog-store";
import { useDialogOpen } from "@/stores/dialog-store";
import { SupportRequestStatus } from "@/types/enum";
import { ApologyCodeDialogContent } from "../components/apology-code-dialog-content";
import { OrderRefundProposals } from "../components/order-refund-proposals";
import { ProposeRefundDialogContent } from "../components/propose-refund-dialog-content";
import { StaffMessageThread } from "../components/staff-message-thread";
import { StaffReplyForm } from "../components/staff-reply-form";
import { SupportApologyCodeCard } from "../components/support-apology-code-card";
import { SupportAssigneeControl } from "../components/support-assignee-control";
import { SupportStatusControl } from "../components/support-status-control";
import { useAdminSupportRequestDetail, useResendSupportTicketEmail } from "../hooks/use-admin-support";
import type { StaffSupportRequestDto } from "../types";
import { getStaffSupportTypeLabel } from "../utils/support-labels";

const SupportRequestDetailSkeleton = () => (
  <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_22rem]">
    <div className="space-y-4">
      <CardSkeleton className="h-24" />
      <CardSkeleton className="h-24" />
      <CardSkeleton className="h-40" />
    </div>
    <div className="space-y-4">
      <CardSkeleton className="h-40" />
      <CardSkeleton className="h-40" />
    </div>
  </div>
);

const SidebarSection = ({ title, children }: { title: string; children: ReactNode }) => (
  <section className="space-y-3 cut-md bg-card p-4">
    <h2 className="t-h3 text-foreground">{title}</h2>
    {children}
  </section>
);

const SupportRequestDetailContent = ({ request }: { request: StaffSupportRequestDto }) => {
  const openDialog = useDialogOpen();
  const openAlertDialog = useAlertDialogOpen();
  const resend = useResendSupportTicketEmail(request.supportRequestId);
  const isResolved = request.status === SupportRequestStatus.Resolved;

  const handleResend = () =>
    openAlertDialog({
      title: "Gửi lại email vé?",
      description: `Email vé của đơn ${request.orderCode} sẽ được gửi lại tới ${
        request.customerEmail ?? "email của khách"
      }.`,
      confirmText: "Gửi lại",
      onConfirm: async () => {
        await resend.mutateAsync();
      },
    });

  return (
    <div className="space-y-6">
      <div className="space-y-2">
        <div className="flex flex-wrap items-center gap-2">
          <h1 className="t-h2 text-foreground">{request.subject}</h1>
          <StaffSupportRequestStatusBadge status={request.status} />
          {request.isOverdue && !isResolved && <Badge variant="destructive">Quá hạn</Badge>}
        </div>
        <p className="text-sm text-muted-foreground">
          {getStaffSupportTypeLabel(request.type)} · Đơn{" "}
          <Link
            to={ROUTES.admin.orderDetail(request.orderId)}
            className="t-code text-foreground underline-offset-4 hover:underline"
          >
            {request.orderCode}
          </Link>{" "}
          · {request.customerName || "Khách vãng lai"}
          {request.customerEmail ? ` (${request.customerEmail})` : ""} · Gửi {formatDateTime(request.createdAt)} · Hạn{" "}
          {formatDateTime(request.dueAt)}
        </p>
      </div>

      <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_22rem]">
        <div className="space-y-4">
          <StaffMessageThread messages={request.messages} />
          {isResolved ? (
            <p className="cut-md bg-card p-4 text-sm text-muted-foreground" data-testid="staff-support-resolved">
              Yêu cầu đã được giải quyết. Đổi trạng thái để mở lại nếu cần trao đổi thêm.
            </p>
          ) : (
            <StaffReplyForm requestId={request.supportRequestId} status={request.status} />
          )}
        </div>

        <aside className="space-y-4">
          <SidebarSection title="Xử lý">
            <div className="space-y-1.5">
              <p className="text-sm font-medium text-foreground">Người phụ trách</p>
              <SupportAssigneeControl request={request} disabled={isResolved} />
            </div>
            <div className="space-y-1.5">
              <p className="text-sm font-medium text-foreground">Trạng thái</p>
              <SupportStatusControl requestId={request.supportRequestId} status={request.status} />
            </div>
          </SidebarSection>

          <SidebarSection title="Hành động">
            <div className="flex flex-col gap-2">
              <Button
                variant="outline"
                disabled={isResolved || request.apologyCode != null}
                onClick={() =>
                  openDialog({
                    title: "Cấp mã quà xin lỗi",
                    description: `Yêu cầu của ${request.customerName ?? "khách"} · đơn ${request.orderCode}`,
                    content: <ApologyCodeDialogContent requestId={request.supportRequestId} />,
                  })
                }
              >
                <Gift className="size-4" />
                Cấp mã quà xin lỗi
              </Button>
              <Button variant="outline" disabled={isResolved || resend.isPending} onClick={handleResend}>
                <MailCheck className="size-4" />
                Gửi lại email vé
              </Button>
              <Button
                variant="outline"
                onClick={() =>
                  openDialog({
                    title: "Đề xuất hoàn tiền ngoại lệ",
                    description: `Đơn ${request.orderCode}`,
                    content: <ProposeRefundDialogContent orderId={request.orderId} />,
                  })
                }
              >
                <HandCoins className="size-4" />
                Đề xuất hoàn ngoại lệ
              </Button>
            </div>
            {request.apologyCode && <SupportApologyCodeCard code={request.apologyCode} />}
          </SidebarSection>

          <SidebarSection title="Hoàn tiền ngoại lệ của đơn">
            <OrderRefundProposals orderId={request.orderId} />
          </SidebarSection>
        </aside>
      </div>
    </div>
  );
};

const AdminSupportRequestDetailPage = () => {
  const { id } = useParams<{ id: string }>();
  const requestQuery = useAdminSupportRequestDetail(Number(id));

  return (
    <div className="space-y-4">
      <div>
        <ButtonLink to={ROUTES.admin.supportRequests} variant="ghost" size="sm">
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
            backTo={ROUTES.admin.supportRequests}
          />
        }
        errorTitle="Không tải được yêu cầu hỗ trợ"
      >
        {(request) => <SupportRequestDetailContent request={request} />}
      </QueryState>
    </div>
  );
};

export default AdminSupportRequestDetailPage;
