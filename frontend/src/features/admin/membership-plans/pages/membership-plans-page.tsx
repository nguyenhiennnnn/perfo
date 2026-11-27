import { Crown, Plus } from "lucide-react";

import { PageHeader } from "@/components/common/page-header";
import { Button } from "@/components/ui/button";
import { DataTable } from "@/components/common/data-table/data-table";
import { EnumSelectFilter } from "@/components/common/filter/enum-select-filter";
import { useDialogClose, useDialogOpen } from "@/stores/dialog-store";
import { useAlertDialogOpen } from "@/stores/alert-dialog-store";
import { useAdminMembershipPlanParams } from "@/hooks/use-query-params";
import { useDebouncedValue } from "@/hooks/use-debounced-value";
import { useTablePagination } from "@/hooks/use-table-pagination";
import { DEFAULT_PAGE, IRREVERSIBLE_ACTION_MESSAGE, STATUS_LABEL } from "@/config/default-constants";

import {
  useAdminDeleteMembershipPlan,
  useAdminMembershipPlans,
} from "../hooks/use-admin-membership-plans";
import { createMembershipPlanColumns } from "../components/membership-plan-columns";
import { MembershipPlanForm } from "../components/forms/membership-plan-form";
import type { AdminMembershipPlanDto } from "../types";
import { PLAN_ACTIVE_FILTER_OPTIONS, parseActiveFilter } from "../utils/membership-plan-admin-labels";

const AdminMembershipPlansPage = () => {
  const [params, setParams] = useAdminMembershipPlanParams();
  const { pagination, onPaginationChange } = useTablePagination(params, setParams);

  const plans = useAdminMembershipPlans({
    search: useDebouncedValue(params.search) || undefined,
    isActive: parseActiveFilter(params.isActive),
    page: pagination.pageIndex + 1,
    pageSize: pagination.pageSize,
  });

  const deletePlan = useAdminDeleteMembershipPlan();

  const openDialog = useDialogOpen();
  const closeDialog = useDialogClose();
  const openAlertDialog = useAlertDialogOpen();

  const handleCreate = () => {
    openDialog({
      title: "Tạo gói thành viên",
      description: "Đặt giá, thời hạn, quyền lợi và giá trị quy đổi của vé miễn phí.",
      content: <MembershipPlanForm onSuccess={closeDialog} />,
      size: "lg",
    });
  };

  const handleEdit = (plan: AdminMembershipPlanDto) => {
    openDialog({
      title: "Cập nhật gói thành viên",
      description: "Mọi thay đổi được ghi vào nhật ký thao tác.",
      content: <MembershipPlanForm plan={plan} onSuccess={closeDialog} />,
      size: "lg",
    });
  };

  const handleDelete = (plan: AdminMembershipPlanDto) => {
    openAlertDialog({
      title: `Xóa gói "${plan.name}"?`,
      description: IRREVERSIBLE_ACTION_MESSAGE,
      confirmText: "Xóa",
      variant: "destructive",
      onConfirm: async () => {
        await deletePlan.mutateAsync(plan.membershipPlanId);
      },
    });
  };

  const columns = createMembershipPlanColumns({ onEdit: handleEdit, onDelete: handleDelete });

  return (
    <div>
      <PageHeader
        title="Gói thành viên"
        description="Các gói trả phí theo tháng hoặc quý: giảm giá vé và combo, vé miễn phí mỗi tháng, đặt sớm suất đặc biệt."
        action={
          <Button onClick={handleCreate}>
            <Plus className="size-4" />
            <span>Tạo gói</span>
          </Button>
        }
      />

      <DataTable
        columns={columns}
        data={plans.data?.items ?? []}
        isLoading={plans.isLoading}
        isError={plans.isError}
        error={plans.error}
        onRetry={() => void plans.refetch()}
        isRefreshing={plans.isPlaceholderData}
        totalItems={plans.data?.totalItems ?? 0}
        pagination={pagination}
        onPaginationChange={onPaginationChange}
        searchValue={params.search}
        onSearchChange={(value) => setParams({ search: value, page: DEFAULT_PAGE })}
        searchPlaceholder="Tìm theo tên gói..."
        hasActiveFilters={Boolean(params.search) || params.isActive !== null}
        onClearFilters={() => setParams({ search: "", isActive: null, page: DEFAULT_PAGE })}
        toolbarFilters={
          <EnumSelectFilter
            value={params.isActive}
            onChange={(isActive) => setParams({ isActive, page: DEFAULT_PAGE })}
            options={PLAN_ACTIVE_FILTER_OPTIONS}
            placeholder={STATUS_LABEL}
            allLabel="Tất cả trạng thái"
          />
        }
        empty={{
          icon: Crown,
          title: "Chưa có gói thành viên",
          description: "Tạo gói trả phí để khách hàng thân thiết nhận ưu đãi định kỳ.",
          actionLabel: "Tạo gói",
          onAction: handleCreate,
        }}
      />
    </div>
  );
};

export default AdminMembershipPlansPage;
