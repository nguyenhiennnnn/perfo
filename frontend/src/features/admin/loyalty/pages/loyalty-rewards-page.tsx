import { Gift, Plus } from "lucide-react";

import { PageHeader } from "@/components/common/page-header";
import { Button } from "@/components/ui/button";
import { DataTable } from "@/components/common/data-table/data-table";
import { EnumSelectFilter } from "@/components/common/filter/enum-select-filter";
import { useDialogClose, useDialogOpen } from "@/stores/dialog-store";
import { useAlertDialogOpen } from "@/stores/alert-dialog-store";
import { useAdminLoyaltyRewardParams } from "@/hooks/use-query-params";
import { useDebouncedValue } from "@/hooks/use-debounced-value";
import { useTablePagination } from "@/hooks/use-table-pagination";
import { DEFAULT_PAGE, IRREVERSIBLE_ACTION_MESSAGE, STATUS_LABEL } from "@/config/default-constants";

import {
  useAdminDeleteLoyaltyReward,
  useAdminLoyaltyRewards,
} from "../hooks/use-admin-loyalty-rewards";
import { createRewardColumns } from "../components/reward-columns";
import { RewardForm } from "../components/forms/reward-form";
import type { AdminLoyaltyRewardDto } from "../types";
import {
  ACTIVE_FILTER_OPTIONS,
  REWARD_TYPE_OPTIONS,
  parseActiveFilter,
} from "../utils/loyalty-admin-labels";

const typeFilterOptions = REWARD_TYPE_OPTIONS.map((o) => ({ value: o.value, label: o.label }));

const AdminLoyaltyRewardsPage = () => {
  const [params, setParams] = useAdminLoyaltyRewardParams();
  const { pagination, onPaginationChange } = useTablePagination(params, setParams);

  const rewards = useAdminLoyaltyRewards({
    search: useDebouncedValue(params.search) || undefined,
    type: params.type ?? undefined,
    isActive: parseActiveFilter(params.isActive),
    page: pagination.pageIndex + 1,
    pageSize: pagination.pageSize,
  });

  const deleteReward = useAdminDeleteLoyaltyReward();

  const openDialog = useDialogOpen();
  const closeDialog = useDialogClose();
  const openAlertDialog = useAlertDialogOpen();

  const handleCreate = () => {
    openDialog({
      title: "Tạo quà",
      description: "Chọn loại quà, số điểm cần để đổi, số lượng và thời gian cho phép đổi.",
      content: <RewardForm onSuccess={closeDialog} />,
      size: "lg",
    });
  };

  const handleEdit = (reward: AdminLoyaltyRewardDto) => {
    openDialog({
      title: "Cập nhật quà",
      description: "Mọi thay đổi được ghi vào nhật ký thao tác.",
      content: <RewardForm reward={reward} onSuccess={closeDialog} />,
      size: "lg",
    });
  };

  const handleDelete = (reward: AdminLoyaltyRewardDto) => {
    openAlertDialog({
      title: `Xóa quà "${reward.name}"?`,
      description: IRREVERSIBLE_ACTION_MESSAGE,
      confirmText: "Xóa",
      variant: "destructive",
      onConfirm: async () => {
        await deleteReward.mutateAsync(reward.rewardId);
      },
    });
  };

  const columns = createRewardColumns({ onEdit: handleEdit, onDelete: handleDelete });

  return (
    <div>
      <PageHeader
        title="Quản lý quà"
        description="Danh mục quà khách đổi bằng điểm: vé miễn phí, combo miễn phí và mã giảm giá."
        action={
          <Button onClick={handleCreate}>
            <Plus className="size-4" />
            <span>Tạo quà</span>
          </Button>
        }
      />

      <DataTable
        columns={columns}
        data={rewards.data?.items ?? []}
        isLoading={rewards.isLoading}
        isError={rewards.isError}
        error={rewards.error}
        onRetry={() => void rewards.refetch()}
        isRefreshing={rewards.isPlaceholderData}
        totalItems={rewards.data?.totalItems ?? 0}
        pagination={pagination}
        onPaginationChange={onPaginationChange}
        searchValue={params.search}
        onSearchChange={(value) => setParams({ search: value, page: DEFAULT_PAGE })}
        searchPlaceholder="Tìm theo tên quà..."
        hasActiveFilters={Boolean(params.search) || params.type !== null || params.isActive !== null}
        onClearFilters={() =>
          setParams({ search: "", type: null, isActive: null, page: DEFAULT_PAGE })
        }
        toolbarFilters={
          <>
            <EnumSelectFilter
              value={params.type}
              onChange={(type) => setParams({ type, page: DEFAULT_PAGE })}
              options={typeFilterOptions}
              placeholder="Loại quà"
              allLabel="Tất cả loại"
            />
            <EnumSelectFilter
              value={params.isActive}
              onChange={(isActive) => setParams({ isActive, page: DEFAULT_PAGE })}
              options={ACTIVE_FILTER_OPTIONS}
              placeholder={STATUS_LABEL}
              allLabel="Tất cả trạng thái"
            />
          </>
        }
        empty={{
          icon: Gift,
          title: "Chưa có quà",
          description: "Tạo quà để khách có thể dùng điểm tích lũy đổi ưu đãi.",
          actionLabel: "Tạo quà",
          onAction: handleCreate,
        }}
      />
    </div>
  );
};

export default AdminLoyaltyRewardsPage;
