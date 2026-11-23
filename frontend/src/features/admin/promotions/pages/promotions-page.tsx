import { BadgePercent, KeyRound, Plus } from "lucide-react";
import { useNavigate } from "react-router";

import { PageHeader } from "@/components/common/page-header";
import { Button } from "@/components/ui/button";
import { DataTable } from "@/components/common/data-table/data-table";
import { EnumSelectFilter } from "@/components/common/filter/enum-select-filter";
import { ROUTES } from "@/config/routes";
import { useDialogClose, useDialogOpen } from "@/stores/dialog-store";
import { useAlertDialogOpen } from "@/stores/alert-dialog-store";
import { useAdminPromotionParams } from "@/hooks/use-query-params";
import { useDebouncedValue } from "@/hooks/use-debounced-value";
import { useTablePagination } from "@/hooks/use-table-pagination";
import { DEFAULT_PAGE, IRREVERSIBLE_ACTION_MESSAGE, STATUS_LABEL } from "@/config/default-constants";

import {
  useAdminDeletePromotion,
  useAdminPausePromotion,
  useAdminPromotions,
  useAdminPromotionUsageMap,
  useAdminResumePromotion,
} from "../hooks/use-admin-promotions";
import { createPromotionColumns } from "../components/promotion-columns";
import { PromotionForm } from "../components/forms/promotion-form";
import { PersonalCodeBulkForm } from "../components/forms/personal-code-bulk-form";
import type { PromotionDto } from "../types";
import {
  ACTIVE_FILTER_OPTIONS,
  PROMOTION_TYPE_OPTIONS,
  parseActiveFilter,
} from "../utils/promotion-admin-labels";

const typeFilterOptions = PROMOTION_TYPE_OPTIONS.map((o) => ({ value: o.value, label: o.label }));

const AdminPromotionsPage = () => {
  const navigate = useNavigate();
  const [params, setParams] = useAdminPromotionParams();
  const { pagination, onPaginationChange } = useTablePagination(params, setParams);

  const filter = {
    search: useDebouncedValue(params.search) || undefined,
    type: params.type ?? undefined,
    isActive: parseActiveFilter(params.isActive),
  };

  const promotions = useAdminPromotions({
    ...filter,
    page: pagination.pageIndex + 1,
    pageSize: pagination.pageSize,
  });
  const usage = useAdminPromotionUsageMap(filter);

  const pausePromotion = useAdminPausePromotion();
  const resumePromotion = useAdminResumePromotion();
  const deletePromotion = useAdminDeletePromotion();

  const openDialog = useDialogOpen();
  const closeDialog = useDialogClose();
  const openAlertDialog = useAlertDialogOpen();

  const handleCreate = () => {
    openDialog({
      title: "Tạo khuyến mãi",
      description: "Chọn loại khuyến mãi, mức ưu đãi, điều kiện và giới hạn sử dụng.",
      content: <PromotionForm onSuccess={closeDialog} />,
      size: "lg",
    });
  };

  const handleBulk = () => {
    openDialog({
      title: "Tạo mã cá nhân hàng loạt",
      description: "Mỗi khách được chọn nhận một mã dùng một lần với cùng mức ưu đãi.",
      content: <PersonalCodeBulkForm onClose={closeDialog} />,
      size: "lg",
    });
  };

  const handleEdit = (promotion: PromotionDto) => {
    openDialog({
      title: "Cập nhật khuyến mãi",
      description: "Mọi thay đổi được ghi vào nhật ký thao tác.",
      content: <PromotionForm promotion={promotion} onSuccess={closeDialog} />,
      size: "lg",
    });
  };

  const handleToggleActive = (promotion: PromotionDto) => {
    if (promotion.isActive) pausePromotion.mutate(promotion.promotionId);
    else resumePromotion.mutate(promotion.promotionId);
  };

  const handleDelete = (promotion: PromotionDto) => {
    openAlertDialog({
      title: `Xóa khuyến mãi "${promotion.code}"?`,
      description: IRREVERSIBLE_ACTION_MESSAGE,
      confirmText: "Xóa",
      variant: "destructive",
      onConfirm: async () => {
        await deletePromotion.mutateAsync(promotion.promotionId);
      },
    });
  };

  const columns = createPromotionColumns({
    usageMap: usage.data,
    onEdit: handleEdit,
    onToggleActive: handleToggleActive,
    onDelete: handleDelete,
    onViewRedemptions: (promotion) =>
      void navigate(ROUTES.admin.promotionRedemptions(promotion.promotionId)),
    onViewReport: (promotion) =>
      void navigate(ROUTES.admin.promotionReport(promotion.promotionId)),
  });

  return (
    <div>
      <PageHeader
        title="Khuyến mãi"
        description="Quản lý mã khuyến mãi, chiến dịch tự động áp dụng và mã cá nhân."
        action={
          <div className="flex flex-wrap gap-2">
            <Button variant="outline" onClick={handleBulk}>
              <KeyRound className="size-4" />
              <span>Tạo mã cá nhân hàng loạt</span>
            </Button>
            <Button onClick={handleCreate}>
              <Plus className="size-4" />
              <span>Tạo khuyến mãi</span>
            </Button>
          </div>
        }
      />

      <DataTable
        columns={columns}
        data={promotions.data?.items ?? []}
        isLoading={promotions.isLoading}
        isError={promotions.isError}
        error={promotions.error}
        onRetry={() => void promotions.refetch()}
        isRefreshing={promotions.isPlaceholderData}
        totalItems={promotions.data?.totalItems ?? 0}
        pagination={pagination}
        onPaginationChange={onPaginationChange}
        searchValue={params.search}
        onSearchChange={(value) => setParams({ search: value, page: DEFAULT_PAGE })}
        searchPlaceholder="Tìm theo mã hoặc tên khuyến mãi..."
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
              placeholder="Loại khuyến mãi"
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
          icon: BadgePercent,
          title: "Chưa có khuyến mãi",
          description: "Tạo khuyến mãi để thu hút khách và đo hiệu quả từng chiến dịch.",
          actionLabel: "Tạo khuyến mãi",
          onAction: handleCreate,
        }}
      />
    </div>
  );
};

export default AdminPromotionsPage;
