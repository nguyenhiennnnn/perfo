import { PackageX } from "lucide-react";

import { PageHeader } from "@/components/common/page-header";
import { DataTable } from "@/components/common/data-table/data-table";
import { DEFAULT_PAGE } from "@/config/default-constants";
import { useAdminListParams } from "@/hooks/use-query-params";
import { useTablePagination } from "@/hooks/use-table-pagination";

import { createUnclaimedComboColumns } from "../components/unclaimed-combo-columns";
import { useAdminUnclaimedComboOrders } from "../hooks/use-admin-inventory";

const AdminUnclaimedComboOrdersPage = () => {
  const [params, setParams] = useAdminListParams();
  const { pagination, onPaginationChange } = useTablePagination(params, setParams);

  const orders = useAdminUnclaimedComboOrders({
    page: pagination.pageIndex + 1,
    pageSize: pagination.pageSize,
    search: params.search || undefined,
  });

  return (
    <div>
      <PageHeader
        title="Combo chưa nhận"
        description="Đơn chỉ có combo đã thanh toán, quá ngày bán mà khách chưa nhận đủ."
      />

      <DataTable
        columns={createUnclaimedComboColumns()}
        data={orders.data?.items ?? []}
        isLoading={orders.isLoading}
        isError={orders.isError}
        error={orders.error}
        onRetry={() => void orders.refetch()}
        isRefreshing={orders.isPlaceholderData}
        totalItems={orders.data?.totalItems ?? 0}
        pagination={pagination}
        onPaginationChange={onPaginationChange}
        searchValue={params.search}
        onSearchChange={(value) => setParams({ search: value, page: DEFAULT_PAGE })}
        searchPlaceholder="Tìm theo mã đơn, tên hoặc số điện thoại..."
        empty={{
          icon: PackageX,
          title: "Không có đơn combo chưa nhận",
          description: "Tất cả đơn combo quá ngày đã được giao đủ.",
        }}
      />
    </div>
  );
};

export default AdminUnclaimedComboOrdersPage;
