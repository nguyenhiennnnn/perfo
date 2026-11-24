import { Clock } from "lucide-react";

import { PageHeader } from "@/components/common/page-header";
import { DataTable } from "@/components/common/data-table/data-table";
import { EnumSelectFilter } from "@/components/common/filter/enum-select-filter";
import { DateFilterPicker } from "@/components/common/filter/date-filter-picker";
import type { EnumOption } from "@/lib/enum-options";
import { stripNulls } from "@/lib/query-params";
import { DEFAULT_PAGE } from "@/config/default-constants";
import { useDialogOpen } from "@/stores/dialog-store";
import { useAdminCounterShiftParams } from "@/hooks/use-query-params";
import { useTablePagination } from "@/hooks/use-table-pagination";
import { useAdminEmployees } from "@/features/admin/employees/hooks/use-admin-employee";

import { useAdminCounterShifts } from "../hooks/use-admin-counter-shifts";
import { createCounterShiftColumns } from "../components/counter-shift-columns";
import { AdminCloseShiftDialogContent } from "../components/admin-close-shift-dialog";
import { OpenShiftsPanel } from "../components/open-shifts-panel";
import {
  COUNTER_SHIFT_STATUS_CLOSED,
  COUNTER_SHIFT_STATUS_OPEN,
  type CounterShift,
} from "../types";

const EMPLOYEE_OPTIONS_PAGE_SIZE = 100;

const STATUS_OPTIONS: EnumOption[] = [
  { value: COUNTER_SHIFT_STATUS_OPEN, label: "Đang mở" },
  { value: COUNTER_SHIFT_STATUS_CLOSED, label: "Đã đóng" },
];

const AdminCounterShiftsPage = () => {
  const [params, setParams] = useAdminCounterShiftParams();
  const { pagination, onPaginationChange } = useTablePagination(params, setParams);
  const shifts = useAdminCounterShifts(stripNulls(params));
  const employees = useAdminEmployees({ pageSize: EMPLOYEE_OPTIONS_PAGE_SIZE });
  const openDialog = useDialogOpen();

  const employeeOptions: EnumOption[] = (employees.data?.items ?? []).map((employee) => ({
    value: String(employee.employeeId),
    label: employee.fullName,
  }));

  const handleCloseByAdmin = (shift: CounterShift) =>
    openDialog({
      title: "Đóng thay ca quầy",
      description: "Thao tác được ghi vào nhật ký.",
      content: <AdminCloseShiftDialogContent shift={shift} />,
    });

  const columns = createCounterShiftColumns({ onCloseByAdmin: handleCloseByAdmin });

  return (
    <div>
      <PageHeader
        title="Ca quầy"
        description="Theo dõi ca của nhân viên quầy, đối chiếu tiền mặt và đóng thay ca bị bỏ quên."
      />

      <OpenShiftsPanel onCloseByAdmin={handleCloseByAdmin} />

      <div className="mb-4 grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
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
        <EnumSelectFilter
          value={params.employeeId}
          onChange={(employeeId) => setParams({ employeeId, page: DEFAULT_PAGE })}
          options={employeeOptions}
          placeholder="Nhân viên"
          allLabel="Tất cả nhân viên"
        />
        <EnumSelectFilter
          value={params.status}
          onChange={(status) => setParams({ status, page: DEFAULT_PAGE })}
          options={STATUS_OPTIONS}
          placeholder="Trạng thái"
          allLabel="Tất cả trạng thái"
        />
      </div>

      <DataTable
        columns={columns}
        data={shifts.data?.items ?? []}
        isLoading={shifts.isLoading}
        isError={shifts.isError}
        error={shifts.error}
        onRetry={() => void shifts.refetch()}
        isRefreshing={shifts.isPlaceholderData}
        totalItems={shifts.data?.totalItems ?? 0}
        pagination={pagination}
        onPaginationChange={onPaginationChange}
        empty={{
          icon: Clock,
          title: "Chưa có ca quầy",
          description: "Ca xuất hiện khi nhân viên mở ca bán hàng tại quầy.",
        }}
        hasActiveFilters={
          params.employeeId !== null ||
          params.status !== null ||
          params.fromDate !== null ||
          params.toDate !== null
        }
        onClearFilters={() =>
          setParams({
            employeeId: null,
            status: null,
            fromDate: null,
            toDate: null,
            page: DEFAULT_PAGE,
          })
        }
      />
    </div>
  );
};

export default AdminCounterShiftsPage;
