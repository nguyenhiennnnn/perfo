import { useMemo } from "react";
import { ClipboardCheck } from "lucide-react";
import { DataTable } from "@/components/common/data-table/data-table";
import { EnumSelectFilter } from "@/components/common/filter/enum-select-filter";
import { DEFAULT_PAGE, LOOKUP_PAGE_SIZE } from "@/config/default-constants";
import { useAdminUsersSelect } from "@/features/admin/users/hooks/use-admin-users";
import { useAdminReconciliationParams } from "@/hooks/use-query-params";
import { useTablePagination } from "@/hooks/use-table-pagination";
import { stripNulls } from "@/lib/query-params";
import { useDialogClose, useDialogOpen } from "@/stores/dialog-store";
import { useReconciliationItems, useReconciliationSessions } from "../hooks/use-reconciliation";
import type { ReconciliationItemDto } from "../types";
import {
  ITEM_RESULT_OPTIONS,
  RESOLUTION_OPTIONS,
  getGatewayLabel,
} from "../utils/reconciliation-labels";
import { createItemColumns } from "./item-columns";
import { ReconciliationItemForm } from "./reconciliation-item-form";

interface ReconciliationItemsTableProps {
  fixedSessionId?: number;
}

export const ReconciliationItemsTable = ({ fixedSessionId }: ReconciliationItemsTableProps) => {
  const [params, setParams] = useAdminReconciliationParams();
  const { pagination, onPaginationChange } = useTablePagination(params, setParams);
  const openDialog = useDialogOpen();
  const closeDialog = useDialogClose();

  const staff = useAdminUsersSelect({ availableOnly: false });
  const sessions = useReconciliationSessions({ page: DEFAULT_PAGE, pageSize: LOOKUP_PAGE_SIZE });

  const staffOptions = useMemo(
    () => (staff.data ?? []).map((u) => ({ value: String(u.userId), label: u.fullName })),
    [staff.data],
  );
  const sessionOptions = useMemo(
    () =>
      (sessions.data?.items ?? []).map((s) => ({
        value: String(s.sessionId),
        label: `${getGatewayLabel(s.gateway)} #${s.sessionId}`,
      })),
    [sessions.data],
  );

  const sessionId = fixedSessionId ?? params.sessionId ?? undefined;
  const filter = stripNulls({
    result: params.result,
    resolution: params.resolution,
    assignedToUserId: params.assignee,
  });

  const items = useReconciliationItems({
    ...filter,
    sessionId,
    page: pagination.pageIndex + 1,
    pageSize: pagination.pageSize,
  });

  const columns = useMemo(
    () =>
      createItemColumns({
        onEdit: (item: ReconciliationItemDto) =>
          openDialog({
            title: "Xử lý dòng bất thường",
            description: "Ghi chú, gán người xử lý và cập nhật trạng thái.",
            content: <ReconciliationItemForm item={item} onSuccess={closeDialog} />,
          }),
      }),
    [openDialog, closeDialog],
  );

  const hasActiveFilters =
    params.result !== null ||
    params.resolution !== null ||
    params.assignee !== null ||
    (fixedSessionId === undefined && params.sessionId !== null);

  return (
    <div className="space-y-4">
      <div
        className={
          fixedSessionId === undefined
            ? "grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4"
            : "grid grid-cols-1 gap-3 sm:grid-cols-3"
        }
      >
        <EnumSelectFilter
          value={params.result}
          onChange={(result) => void setParams({ result, page: DEFAULT_PAGE })}
          options={ITEM_RESULT_OPTIONS}
          placeholder="Loại kết quả"
          allLabel="Tất cả loại kết quả"
        />
        <EnumSelectFilter
          value={params.resolution}
          onChange={(resolution) => void setParams({ resolution, page: DEFAULT_PAGE })}
          options={RESOLUTION_OPTIONS}
          placeholder="Trạng thái xử lý"
          allLabel="Tất cả trạng thái"
        />
        <EnumSelectFilter
          value={params.assignee !== null ? String(params.assignee) : null}
          onChange={(value) =>
            void setParams({ assignee: value === null ? null : Number(value), page: DEFAULT_PAGE })
          }
          options={staffOptions}
          placeholder="Người xử lý"
          allLabel="Tất cả người xử lý"
        />
        {fixedSessionId === undefined && (
          <EnumSelectFilter
            value={params.sessionId !== null ? String(params.sessionId) : null}
            onChange={(value) =>
              void setParams({ sessionId: value === null ? null : Number(value), page: DEFAULT_PAGE })
            }
            options={sessionOptions}
            placeholder="Phiên đối soát"
            allLabel="Tất cả phiên"
          />
        )}
      </div>

      <DataTable
        columns={columns}
        data={items.data?.items ?? []}
        isLoading={items.isLoading}
        isError={items.isError}
        error={items.error}
        onRetry={() => void items.refetch()}
        isRefreshing={items.isPlaceholderData}
        totalItems={items.data?.totalItems ?? 0}
        pagination={pagination}
        onPaginationChange={onPaginationChange}
        empty={{
          icon: ClipboardCheck,
          title: "Không có dòng bất thường",
          description: "Các dòng lệch sau khi đối soát sẽ hiển thị tại đây để xử lý.",
        }}
        hasActiveFilters={hasActiveFilters}
        onClearFilters={() =>
          void setParams({
            result: null,
            resolution: null,
            assignee: null,
            sessionId: null,
            page: DEFAULT_PAGE,
          })
        }
      />
    </div>
  );
};
