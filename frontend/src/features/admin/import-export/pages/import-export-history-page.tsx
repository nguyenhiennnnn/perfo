import { PageHeader } from "@/components/common/page-header";
import { DataTable } from "@/components/common/data-table/data-table";
import { EnumSelectFilter } from "@/components/common/filter/enum-select-filter";
import { DEFAULT_PAGE, ALL_STATUSES_LABEL, STATUS_LABEL } from "@/config/default-constants";
import { useAdminImportExportJobParams } from "@/hooks/use-query-params";
import { useTablePagination } from "@/hooks/use-table-pagination";
import {
  importExportJobStatusOptions,
  importExportJobTypeOptions,
  importExportModuleOptions,
} from "@/lib/enum-options";
import { stripNulls } from "@/lib/query-params";
import { useAuthUser } from "@/stores/auth-store";

import { createImportExportJobColumns } from "../components/import-export-job-columns";
import { useImportExportJobDetailDialog } from "../hooks/use-import-export-job-detail-dialog";
import {
  useDownloadImportExportJob,
  useImportExportJobs,
} from "../hooks/use-import-export-jobs";
import { History } from "lucide-react";
import { FilterBar } from "@/components/common/filter/filter-bar";

const AdminImportExportHistoryPage = () => {
  const [params, setParams] = useAdminImportExportJobParams();
  const { pagination, onPaginationChange } = useTablePagination(params, setParams);

  const jobs = useImportExportJobs(stripNulls(params));
  const user = useAuthUser();
  const download = useDownloadImportExportJob();
  const openDetail = useImportExportJobDetailDialog();

  const columns = createImportExportJobColumns({
    currentUserId: user?.userId,
    downloadingJobId: download.isPending ? (download.variables?.jobId ?? null) : null,
    onViewDetail: (job) => openDetail(job.jobId),
    onDownload: (job) =>
      download.mutate({
        jobId: job.jobId,
        fallbackFilename: job.fileName ?? `${job.module}.${job.format}`,
      }),
  });

  return (
    <div>
      <PageHeader
        title="Lịch sử Import/Export"
        description="Theo dõi tiến độ, kết quả và tải lại file của các tác vụ nhập/xuất dữ liệu."
      />

      <FilterBar className="mb-4">
        <EnumSelectFilter
          value={params.type}
          onChange={(type) => setParams({ type, page: DEFAULT_PAGE })}
          options={importExportJobTypeOptions}
          placeholder="Loại tác vụ"
          allLabel="Tất cả loại"
        />
        <EnumSelectFilter
          value={params.module}
          onChange={(module) => setParams({ module, page: DEFAULT_PAGE })}
          options={importExportModuleOptions}
          placeholder="Module"
          allLabel="Tất cả module"
        />
        <EnumSelectFilter
          value={params.status}
          onChange={(status) => setParams({ status, page: DEFAULT_PAGE })}
          options={importExportJobStatusOptions}
          placeholder={STATUS_LABEL}
          allLabel={ALL_STATUSES_LABEL}
        />
      </FilterBar>

      <DataTable
        columns={columns}
        data={jobs.data?.items ?? []}
        isLoading={jobs.isLoading}
        isError={jobs.isError}
        error={jobs.error}
        onRetry={() => void jobs.refetch()}
        isRefreshing={jobs.isPlaceholderData}
        totalItems={jobs.data?.totalItems ?? 0}
        pagination={pagination}
        onPaginationChange={onPaginationChange}
        empty={{
          icon: History,
          title: "Chưa có tác vụ nhập hoặc xuất",
          description: "Tác vụ nhập xuất tệp hiện ở đây sau lần chạy đầu tiên.",
        }}
        hasActiveFilters={params.type !== null || params.module !== null || params.status !== null}
        onClearFilters={() => setParams({ type: null, module: null, status: null, page: DEFAULT_PAGE })}
      />
    </div>
  );
};

export default AdminImportExportHistoryPage;
