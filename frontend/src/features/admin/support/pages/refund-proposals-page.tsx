import { HandCoins } from "lucide-react";
import { DataTable } from "@/components/common/data-table/data-table";
import { PageHeader } from "@/components/common/page-header";
import { Button } from "@/components/ui/button";
import { DEFAULT_PAGE } from "@/config/default-constants";
import { useAdminRefundProposalParams } from "@/hooks/use-query-params";
import { useTablePagination } from "@/hooks/use-table-pagination";
import { stripNulls } from "@/lib/query-params";
import { useAuthRole, useAuthUser } from "@/stores/auth-store";
import { ExceptionalRefundStatus, Role } from "@/types/enum";
import { createRefundProposalColumns } from "../components/refund-proposal-columns";
import { useRefundProposalActions } from "../hooks/use-refund-proposal-actions";
import { useRefundProposals } from "../hooks/use-refund-proposals";
import { REFUND_PROPOSAL_STATUS_TABS } from "../utils/refund-proposal-labels";

const AdminRefundProposalsPage = () => {
  const [params, setParams] = useAdminRefundProposalParams();
  const { pagination, onPaginationChange } = useTablePagination(params, setParams);
  const user = useAuthUser();
  const isAdmin = useAuthRole() === Role.Admin;
  const actions = useRefundProposalActions();

  const proposals = useRefundProposals(
    stripNulls({
      status: params.status === "All" ? undefined : params.status,
      orderId: params.orderId,
      page: params.page,
      pageSize: params.pageSize,
    }),
  );

  const columns = createRefundProposalColumns({
    canReview: isAdmin,
    currentUserId: user?.userId,
    onApprove: actions.approve,
    onReject: actions.reject,
  });

  return (
    <div>
      <PageHeader
        title="Hoàn tiền ngoại lệ"
        description="Danh sách đề xuất hoàn tiền ngoài chính sách. Chỉ quản trị được duyệt, và không duyệt đề xuất của chính mình."
      />

      <div className="mb-4 flex flex-wrap gap-2" role="group" aria-label="Lọc theo trạng thái đề xuất">
        {REFUND_PROPOSAL_STATUS_TABS.map((tab) => (
          <Button
            key={tab.value}
            size="sm"
            variant={params.status === tab.value ? "default" : "outline"}
            aria-pressed={params.status === tab.value}
            onClick={() => setParams({ status: tab.value, page: DEFAULT_PAGE })}
          >
            {tab.label}
          </Button>
        ))}
      </div>

      <DataTable
        columns={columns}
        data={proposals.data?.items ?? []}
        isLoading={proposals.isLoading}
        isError={proposals.isError}
        error={proposals.error}
        onRetry={() => void proposals.refetch()}
        isRefreshing={proposals.isPlaceholderData}
        totalItems={proposals.data?.totalItems ?? 0}
        pagination={pagination}
        onPaginationChange={onPaginationChange}
        empty={{
          icon: HandCoins,
          title:
            params.status === ExceptionalRefundStatus.Pending
              ? "Không có đề xuất nào chờ duyệt"
              : "Chưa có đề xuất hoàn tiền ngoại lệ",
          description: "Đề xuất được tạo từ chi tiết yêu cầu hỗ trợ của khách.",
        }}
        hasActiveFilters={params.orderId !== null}
        onClearFilters={() => setParams({ orderId: null, page: DEFAULT_PAGE })}
      />
    </div>
  );
};

export default AdminRefundProposalsPage;
