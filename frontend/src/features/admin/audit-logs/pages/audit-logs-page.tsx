import { ScrollText } from "lucide-react";

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
} from "../hooks/use-admin-audit-logs";
import { createAuditLogColumns } from "../components/audit-log-columns";
import { AuditLogDetailView } from "../components/audit-log-detail-view";
import { getAuditEntityTypeLabel } from "../utils/audit-log-labels";

const AdminAuditLogsPage = () => {
  const [params, setParams] = useAdminAuditLogParams();
  const { pagination, onPaginationChange } = useTablePagination(params, setParams);

  const debouncedEntityId = useDebouncedValue(params.entityId);
  const listParams = stripNulls({ ...params, entityId: debouncedEntityId || undefined });
  const logs = useAdminAuditLogs(listParams);
  const filterOptions = useAdminAuditLogFilterOptions();
  const openDialog = useDialogOpen();

  const entityTypeOptions: EnumOption[] = (filterOptions.data?.entityTypes ?? []).map((value) => ({
    value,
    label: getAuditEntityTypeLabel(value),
  }));
  const actorOptions: EnumOption[] = (filterOptions.data?.actors ?? []).map((actor) => ({
    value: String(actor.userId),
    label: actor.fullName,
  }));

  const handleViewDetail = (log: AuditLogListItemDto) => {
    openDialog({
      title: `Nhật ký #${log.auditLogId}`,
      description: "Giá trị trước và sau của thao tác.",
      content: <AuditLogDetailView auditLogId={log.auditLogId} />,
      size: "lg",
    });
  };

  const columns = createAuditLogColumns({ onViewDetail: handleViewDetail });

  return (
    <div>
      <PageHeader
        title="Nhật ký thao tác"
        description="Tra cứu các thao tác nhạy cảm của quản trị. Nhật ký chỉ được thêm, không thể sửa hoặc xóa."
      />

      <div className="mb-4 grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-5">
        <EnumSelectFilter
          value={params.entityType}
          onChange={(entityType) => setParams({ entityType, page: DEFAULT_PAGE })}
          options={entityTypeOptions}
          placeholder="Đối tượng"
          allLabel="Tất cả đối tượng"
        />
        <Input
          placeholder="Mã đối tượng..."
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
          icon: ScrollText,
          title: "Chưa có nhật ký thao tác",
          description: "Nhật ký xuất hiện khi quản trị thay đổi cấu hình hoặc dữ liệu nhạy cảm.",
        }}
        hasActiveFilters={
          Boolean(params.entityId) ||
          params.entityType !== null ||
          params.actorUserId !== null ||
          params.fromDate !== null ||
          params.toDate !== null
        }
        onClearFilters={() =>
          setParams({
            entityType: null,
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

export default AdminAuditLogsPage;
