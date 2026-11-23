import { useMemo } from "react";
import type { ColumnFiltersState } from "@tanstack/react-table";
import { Plus, SlidersHorizontal } from "lucide-react";
import { useNavigate } from "react-router";

import { PageHeader } from "@/components/common/page-header";
import { Button } from "@/components/ui/button";
import { DataTable } from "@/components/common/data-table/data-table";
import { EnumSelectFilter } from "@/components/common/filter/enum-select-filter";
import { ROUTES } from "@/config/routes";
import { useDialogClose, useDialogOpen } from "@/stores/dialog-store";
import { useAlertDialogOpen } from "@/stores/alert-dialog-store";
import { useAdminPricingRuleParams } from "@/hooks/use-query-params";
import { useTablePagination } from "@/hooks/use-table-pagination";
import { DEFAULT_PAGE, IRREVERSIBLE_ACTION_MESSAGE, LOOKUP_PAGE_SIZE } from "@/config/default-constants";

import { useAdminDeletePricingRule, useAdminPricingRules } from "../hooks/use-admin-pricing-rules";
import { useAdminSpecialDays } from "../hooks/use-admin-special-days";
import { createPricingRuleColumns } from "../components/pricing-rule-columns";
import { PricingRuleForm } from "../components/forms/pricing-rule-form";
import type { PricingRuleDto } from "../types";
import { RULE_TYPE_OPTIONS } from "../utils/pricing-labels";

const ruleTypeFilterOptions = RULE_TYPE_OPTIONS.map((o) => ({ value: o.value, label: o.label }));

const AdminPricingRulesPage = () => {
  const navigate = useNavigate();
  const [params, setParams] = useAdminPricingRuleParams();
  const { pagination, onPaginationChange } = useTablePagination(params, setParams);

  const rules = useAdminPricingRules({
    page: pagination.pageIndex + 1,
    pageSize: pagination.pageSize,
    search: params.search || undefined,
    ruleType: params.ruleType ?? undefined,
  });
  const specialDays = useAdminSpecialDays({ pageSize: LOOKUP_PAGE_SIZE });
  const deleteRule = useAdminDeletePricingRule();

  const openDialog = useDialogOpen();
  const closeDialog = useDialogClose();
  const openAlertDialog = useAlertDialogOpen();

  const specialDayNames = useMemo(
    () => new Map((specialDays.data?.items ?? []).map((d) => [d.specialDayId, d.name] as const)),
    [specialDays.data],
  );

  const handleCreate = () => {
    openDialog({
      title: "Thêm quy tắc giá",
      description: "Chọn loại quy tắc, điều kiện, cách điều chỉnh và phạm vi áp dụng.",
      content: <PricingRuleForm onSuccess={closeDialog} />,
      size: "lg",
    });
  };

  const handleEdit = (rule: PricingRuleDto) => {
    openDialog({
      title: "Cập nhật quy tắc giá",
      description: "Mọi thay đổi được ghi vào nhật ký thao tác.",
      content: <PricingRuleForm rule={rule} onSuccess={closeDialog} />,
      size: "lg",
    });
  };

  const handleDelete = (rule: PricingRuleDto) => {
    openAlertDialog({
      title: `Xóa quy tắc "${rule.name}"?`,
      description: IRREVERSIBLE_ACTION_MESSAGE,
      confirmText: "Xóa",
      variant: "destructive",
      onConfirm: async () => {
        await deleteRule.mutateAsync(rule.pricingRuleId);
      },
    });
  };

  const columns = createPricingRuleColumns({
    specialDayNames,
    onEdit: handleEdit,
    onHistory: (rule) => void navigate(ROUTES.admin.pricingRuleHistoryByRule(rule.pricingRuleId)),
    onDelete: handleDelete,
  });

  const columnFilters: ColumnFiltersState = params.search
    ? [{ id: "name", value: params.search }]
    : [];

  return (
    <div>
      <PageHeader
        title="Quy tắc giá"
        description="Điều chỉnh giá vé theo khung giờ, thứ trong tuần, ngày lễ và mức lấp đầy. Dùng Xem trước giá để kiểm tra kết quả."
        action={
          <Button onClick={handleCreate}>
            <Plus className="size-4" />
            <span>Thêm quy tắc</span>
          </Button>
        }
      />

      <DataTable
        columns={columns}
        data={rules.data?.items ?? []}
        isLoading={rules.isLoading}
        isError={rules.isError}
        error={rules.error}
        onRetry={() => void rules.refetch()}
        isRefreshing={rules.isPlaceholderData}
        totalItems={rules.data?.totalItems ?? 0}
        pagination={pagination}
        onPaginationChange={onPaginationChange}
        columnFilters={columnFilters}
        searchValue={params.search}
        onSearchChange={(value) => setParams({ search: value, page: DEFAULT_PAGE })}
        searchPlaceholder="Tìm theo tên quy tắc..."
        hasActiveFilters={Boolean(params.search) || params.ruleType !== null}
        onClearFilters={() => setParams({ search: "", ruleType: null, page: DEFAULT_PAGE })}
        toolbarFilters={
          <EnumSelectFilter
            value={params.ruleType}
            onChange={(ruleType) => setParams({ ruleType, page: DEFAULT_PAGE })}
            options={ruleTypeFilterOptions}
            placeholder="Loại quy tắc"
            allLabel="Tất cả loại quy tắc"
          />
        }
        empty={{
          icon: SlidersHorizontal,
          title: "Chưa có quy tắc giá",
          description: "Thêm quy tắc để tăng giá giờ cao điểm hoặc giảm giá giờ vắng.",
          actionLabel: "Thêm quy tắc",
          onAction: handleCreate,
        }}
      />
    </div>
  );
};

export default AdminPricingRulesPage;
