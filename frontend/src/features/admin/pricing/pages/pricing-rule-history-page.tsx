import { FileClock } from "lucide-react";

import { Input } from "@/components/ui/input";
import { PageHeader } from "@/components/common/page-header";
import { DataTable } from "@/components/common/data-table/data-table";
import { EnumSelectFilter } from "@/components/common/filter/enum-select-filter";
import { DateFilterPicker } from "@/components/common/filter/date-filter-picker";
import type { EnumOption } from "@/lib/enum-options";
import { stripNulls } from "@/lib/query-params";
import { DEFAULT_PAGE } from "@/config/default-constants";
import { useDialogOpen } from "@/stores/dialog-store";
import { useAdminAuditLogParams } from "@/hooks/use-query-params";
import { useDebouncedValue } from "@/hooks/use-debounced-value";
import { useTablePagination } from "@/hooks/use-table-pagination";
import type { AuditLogListItemDto } from "@/features/admin/audit-logs/types";
import {
  useAdminAuditLogFilterOptions,
  useAdminAuditLogs,
} from "@/features/admin/audit-logs/hooks/use-admin-audit-logs";
import { createAuditLogColumns } from "@/features/admin/audit-logs/components/audit-log-columns";
import { AuditLogDetailView } from "@/features/admin/audit-logs/components/audit-log-detail-view";

const PRICING_RULE_ENTITY_TYPE = "PricingRule";

const AdminPricingRuleHistoryPage = () => {
  const [params, setParams] = useAdminAuditLogParams();
  const { pagination, onPaginationChange } = useTablePagination(params, setParams);

  const debouncedEntityId = useDebouncedValue(params.entityId);
  const listParams = stripNulls({
    ...params,
    entityType: PRICING_RULE_ENTITY_TYPE,
    entityId: debouncedEntityId || undefined,
  });
  const logs = useAdminAuditLogs(listParams);
  const filterOptions = useAdminAuditLogFilterOptions();
  const openDialog = useDialogOpen();

  const actorOptions: EnumOption[] = (filterOptions.data?.actors ?? []).map((actor) => ({
    value: String(actor.userId),
    label: actor.fullName,
  }));

  const handleViewDetail = (log: AuditLogListItemDto) => {
    openDialog({
      title: `Nhật ký #${log.auditLogId}`,
      description: "Giá trị trước và sau của thay đổi quy tắc giá.",
      content: <AuditLogDetailView auditLogId={log.auditLogId} />,
      size: "lg",
    });
  };

  const columns = createAuditLogColumns({ onViewDetail: handleViewDetail });

  return (
    <div>
      <PageHeader
        title="Lịch sử quy tắc giá"
        description="Các lần tạo, sửa, xóa quy tắc giá, lấy từ nhật ký thao tác."
      />

      <div className="mb-4 grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <Input
          placeholder="Mã quy tắc..."
          value={params.entityId}
          onChange={(e) => setParams({ entityId: e.target.value, page: DEFAULT_PAGE })}
        />
        <EnumSelectFilter
          value={params.actorUserId}
          onChange={(actorUserId) => setParams({ actorUserId, page: DEFAULT_PAGE })}
          options={actorOptions}
          placeholder="Người thực hiện"
          allLabel="Tất cả người thực hiện"
        />
        <DateFilterPicker
          value={params.fromDate}
          onChange={(fromDate) => setParams({ fromDate, page: DEFAULT_PAGE })}
          placeholder="Từ ngày"
        />
        <DateFilterPicker
          value={params.toDate}
          onChange={(toDate) => setParams({ toDate, page: DEFAULT_PAGE })}
          placeholder="Đến ngày"
        />
      </div>

      <DataTable
        columns={columns}
        data={logs.data?.items ?? []}
        isLoading={logs.isLoading}
        isError={logs.isError}
        error={logs.error}
        onRetry={() => void logs.refetch()}
        isRefreshing={logs.isPlaceholderData}
        totalItems={logs.data?.totalItems ?? 0}
        pagination={pagination}
        onPaginationChange={onPaginationChange}
        empty={{
          icon: FileClock,
          title: "Chưa có thay đổi quy tắc giá",
          description: "Lịch sử xuất hiện khi quản trị tạo, sửa hoặc xóa quy tắc giá.",
        }}
        hasActiveFilters={
          Boolean(params.entityId) ||
          params.actorUserId !== null ||
          params.fromDate !== null ||
          params.toDate !== null
        }
        onClearFilters={() =>
          setParams({
            entityId: "",
            actorUserId: null,
            fromDate: null,
            toDate: null,
            page: DEFAULT_PAGE,
          })
        }
      />
    </div>
  );
};

export default AdminPricingRuleHistoryPage;
