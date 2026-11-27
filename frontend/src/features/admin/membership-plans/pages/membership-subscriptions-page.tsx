import { Users } from "lucide-react";

import { PageHeader } from "@/components/common/page-header";
import { DataTable } from "@/components/common/data-table/data-table";
import { EnumSelectFilter } from "@/components/common/filter/enum-select-filter";
import { useDialogOpen } from "@/stores/dialog-store";
import { useAdminMembershipSubscriptionParams } from "@/hooks/use-query-params";
import { useDebouncedValue } from "@/hooks/use-debounced-value";
import { useTablePagination } from "@/hooks/use-table-pagination";
import { DEFAULT_PAGE, STATUS_LABEL } from "@/config/default-constants";

import {
  useAdminMembershipPlanLookup,
  useAdminMembershipSubscriptions,
} from "../hooks/use-admin-membership-plans";
import { createMembershipSubscriptionColumns } from "../components/membership-subscription-columns";
import { MembershipRefundDialogContent } from "../components/membership-refund-dialog-content";
import type { AdminMembershipSubscriptionDto } from "../types";
import {
  SUBSCRIPTION_STATUS_FILTER_OPTIONS,
  parsePlanIdFilter,
} from "../utils/membership-plan-admin-labels";

const AdminMembershipSubscriptionsPage = () => {
  const [params, setParams] = useAdminMembershipSubscriptionParams();
  const { pagination, onPaginationChange } = useTablePagination(params, setParams);

  const planLookup = useAdminMembershipPlanLookup();
  const subscriptions = useAdminMembershipSubscriptions({
    search: useDebouncedValue(params.search) || undefined,
    status: params.status ?? undefined,
    planId: parsePlanIdFilter(params.planId),
    page: pagination.pageIndex + 1,
    pageSize: pagination.pageSize,
  });

  const openDialog = useDialogOpen();

  const handleRefund = (subscription: AdminMembershipSubscriptionDto) => {
    openDialog({
      title: "Hoàn ngoại lệ gói thành viên",
      description: "Hoàn tiền ngoài chính sách, dừng doanh thu còn lại và thu hồi vé miễn phí chưa dùng.",
      content: <MembershipRefundDialogContent subscription={subscription} />,
      size: "md",
    });
  };

  const columns = createMembershipSubscriptionColumns({ onRefund: handleRefund });
  const planOptions = (planLookup.data ?? []).map((plan) => ({
    value: String(plan.membershipPlanId),
    label: plan.name,
  }));

  return (
    <div>
      <PageHeader
        title="Hội viên"
        description="Danh sách đăng ký gói thành viên của khách hàng và xử lý hoàn ngoại lệ."
      />

      <DataTable
        columns={columns}
        data={subscriptions.data?.items ?? []}
        isLoading={subscriptions.isLoading}
        isError={subscriptions.isError}
        error={subscriptions.error}
        onRetry={() => void subscriptions.refetch()}
        isRefreshing={subscriptions.isPlaceholderData}
        totalItems={subscriptions.data?.totalItems ?? 0}
        pagination={pagination}
        onPaginationChange={onPaginationChange}
        searchValue={params.search}
        onSearchChange={(value) => setParams({ search: value, page: DEFAULT_PAGE })}
        searchPlaceholder="Tìm theo tên, email, số điện thoại, mã đơn..."
        hasActiveFilters={Boolean(params.search) || params.status !== null || params.planId !== null}
        onClearFilters={() =>
          setParams({ search: "", status: null, planId: null, page: DEFAULT_PAGE })
        }
        toolbarFilters={
          <>
            <EnumSelectFilter
              value={params.status}
              onChange={(status) => setParams({ status, page: DEFAULT_PAGE })}
              options={SUBSCRIPTION_STATUS_FILTER_OPTIONS}
              placeholder={STATUS_LABEL}
              allLabel="Tất cả trạng thái"
            />
            <EnumSelectFilter
              value={params.planId}
              onChange={(planId) => setParams({ planId, page: DEFAULT_PAGE })}
              options={planOptions}
              placeholder="Gói"
              allLabel="Tất cả gói"
            />
          </>
        }
        empty={{
          icon: Users,
          title: "Chưa có hội viên",
          description: "Khách mua gói thành viên sẽ xuất hiện ở đây.",
        }}
      />
    </div>
  );
};

export default AdminMembershipSubscriptionsPage;
