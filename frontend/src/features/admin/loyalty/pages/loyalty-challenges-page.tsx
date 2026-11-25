import { Plus, Trophy } from "lucide-react";

import { PageHeader } from "@/components/common/page-header";
import { Button } from "@/components/ui/button";
import { DataTable } from "@/components/common/data-table/data-table";
import { EnumSelectFilter } from "@/components/common/filter/enum-select-filter";
import { useDialogClose, useDialogOpen } from "@/stores/dialog-store";
import { useAlertDialogOpen } from "@/stores/alert-dialog-store";
import { useAdminLoyaltyChallengeParams } from "@/hooks/use-query-params";
import { useDebouncedValue } from "@/hooks/use-debounced-value";
import { useTablePagination } from "@/hooks/use-table-pagination";
import { getBusinessToday } from "@/lib/business-time";
import { DEFAULT_PAGE, IRREVERSIBLE_ACTION_MESSAGE, STATUS_LABEL } from "@/config/default-constants";

import {
  useAdminDeleteLoyaltyChallenge,
  useAdminLoyaltyChallenges,
} from "../hooks/use-admin-loyalty-challenges";
import { createChallengeColumns } from "../components/challenge-columns";
import { ChallengeForm } from "../components/forms/challenge-form";
import type { AdminLoyaltyChallengeDto } from "../types";
import { ACTIVE_FILTER_OPTIONS, parseActiveFilter } from "../utils/loyalty-admin-labels";

const AdminLoyaltyChallengesPage = () => {
  const [params, setParams] = useAdminLoyaltyChallengeParams();
  const { pagination, onPaginationChange } = useTablePagination(params, setParams);

  const challenges = useAdminLoyaltyChallenges({
    search: useDebouncedValue(params.search) || undefined,
    isActive: parseActiveFilter(params.isActive),
    page: pagination.pageIndex + 1,
    pageSize: pagination.pageSize,
  });

  const deleteChallenge = useAdminDeleteLoyaltyChallenge();

  const openDialog = useDialogOpen();
  const closeDialog = useDialogClose();
  const openAlertDialog = useAlertDialogOpen();

  const handleCreate = () => {
    openDialog({
      title: "Tạo thử thách",
      description: "Đặt kỳ áp dụng, điều kiện hoàn thành và phần thưởng cho khách.",
      content: <ChallengeForm onSuccess={closeDialog} />,
      size: "lg",
    });
  };

  const handleEdit = (challenge: AdminLoyaltyChallengeDto) => {
    openDialog({
      title: "Cập nhật thử thách",
      description: "Mọi thay đổi được ghi vào nhật ký thao tác.",
      content: <ChallengeForm challenge={challenge} onSuccess={closeDialog} />,
      size: "lg",
    });
  };

  const handleDelete = (challenge: AdminLoyaltyChallengeDto) => {
    openAlertDialog({
      title: `Xóa thử thách "${challenge.name}"?`,
      description: IRREVERSIBLE_ACTION_MESSAGE,
      confirmText: "Xóa",
      variant: "destructive",
      onConfirm: async () => {
        await deleteChallenge.mutateAsync(challenge.challengeId);
      },
    });
  };

  const columns = createChallengeColumns({
    today: getBusinessToday(),
    onEdit: handleEdit,
    onDelete: handleDelete,
  });

  return (
    <div>
      <PageHeader
        title="Quản lý thử thách"
        description="Thử thách xem phim theo kỳ, thưởng điểm hoặc quà khi khách hoàn thành."
        action={
          <Button onClick={handleCreate}>
            <Plus className="size-4" />
            <span>Tạo thử thách</span>
          </Button>
        }
      />

      <DataTable
        columns={columns}
        data={challenges.data?.items ?? []}
        isLoading={challenges.isLoading}
        isError={challenges.isError}
        error={challenges.error}
        onRetry={() => void challenges.refetch()}
        isRefreshing={challenges.isPlaceholderData}
        totalItems={challenges.data?.totalItems ?? 0}
        pagination={pagination}
        onPaginationChange={onPaginationChange}
        searchValue={params.search}
        onSearchChange={(value) => setParams({ search: value, page: DEFAULT_PAGE })}
        searchPlaceholder="Tìm theo tên thử thách..."
        hasActiveFilters={Boolean(params.search) || params.isActive !== null}
        onClearFilters={() => setParams({ search: "", isActive: null, page: DEFAULT_PAGE })}
        toolbarFilters={
          <EnumSelectFilter
            value={params.isActive}
            onChange={(isActive) => setParams({ isActive, page: DEFAULT_PAGE })}
            options={ACTIVE_FILTER_OPTIONS}
            placeholder={STATUS_LABEL}
            allLabel="Tất cả trạng thái"
          />
        }
        empty={{
          icon: Trophy,
          title: "Chưa có thử thách",
          description: "Tạo thử thách để khuyến khích khách xem phim thường xuyên hơn.",
          actionLabel: "Tạo thử thách",
          onAction: handleCreate,
        }}
      />
    </div>
  );
};

export default AdminLoyaltyChallengesPage;
