import { Columns3, Presentation, Table2 } from "lucide-react";
import { DataTable } from "@/components/common/data-table/data-table";
import { EnumSelectFilter } from "@/components/common/filter/enum-select-filter";
import { FilterBar, FilterSearch } from "@/components/common/filter/filter-bar";
import { PageHeader } from "@/components/common/page-header";
import { Button } from "@/components/ui/button";
import { DEFAULT_PAGE } from "@/config/default-constants";
import { useDebouncedValue } from "@/hooks/use-debounced-value";
import { useAdminRoomRentalParams } from "@/hooks/use-query-params";
import { useTablePagination } from "@/hooks/use-table-pagination";
import { stripNulls } from "@/lib/query-params";
import { createRoomRentalColumns } from "../components/room-rental-columns";
import { RoomRentalBoard } from "../components/room-rental-board";
import { useAdminRoomRentals } from "../hooks/use-admin-room-rentals";
import { roomRentalStatusOptions } from "../utils/room-rental-admin-labels";

const columns = createRoomRentalColumns();

const RoomRentalTable = () => {
  const [params, setParams] = useAdminRoomRentalParams();
  const { pagination, onPaginationChange } = useTablePagination(params, setParams);
  const debouncedSearch = useDebouncedValue(params.search);

  const requests = useAdminRoomRentals(
    stripNulls({
      status: params.status,
      search: debouncedSearch || undefined,
      page: params.page,
      pageSize: params.pageSize,
    }),
  );

  return (
    <DataTable
      columns={columns}
      data={requests.data?.items ?? []}
      isLoading={requests.isLoading}
      isError={requests.isError}
      error={requests.error}
      onRetry={() => void requests.refetch()}
      isRefreshing={requests.isPlaceholderData}
      totalItems={requests.data?.totalItems ?? 0}
      pagination={pagination}
      onPaginationChange={onPaginationChange}
      empty={{
        icon: Presentation,
        title: "Chưa có yêu cầu thuê phòng",
        description: "Yêu cầu khách gửi từ trang thuê phòng sẽ xuất hiện tại đây.",
      }}
      hasActiveFilters={Boolean(params.search) || params.status !== null}
      onClearFilters={() => setParams({ search: "", status: null, page: DEFAULT_PAGE })}
    />
  );
};

const AdminRoomRentalRequestsPage = () => {
  const [params, setParams] = useAdminRoomRentalParams();
  const debouncedSearch = useDebouncedValue(params.search);
  const isBoard = params.view === "board";

  return (
    <div>
      <PageHeader
        title="Yêu cầu thuê phòng"
        description="Theo dõi yêu cầu của doanh nghiệp, trường học, nhóm bạn từ lúc gửi đến khi hoàn tất."
        action={
          <div className="flex gap-1" role="group" aria-label="Chế độ xem">
            <Button
              size="sm"
              variant={isBoard ? "default" : "outline"}
              aria-pressed={isBoard}
              onClick={() => setParams({ view: "board" })}
            >
              <Columns3 className="size-4" />
              <span>Cột</span>
            </Button>
            <Button
              size="sm"
              variant={isBoard ? "outline" : "default"}
              aria-pressed={!isBoard}
              onClick={() => setParams({ view: "table" })}
            >
              <Table2 className="size-4" />
              <span>Bảng</span>
            </Button>
          </div>
        }
      />

      <FilterBar className="mb-4">
        <FilterSearch
          placeholder="Mã yêu cầu, tên, đơn vị, số điện thoại..."
          value={params.search}
          onChange={(value) => setParams({ search: value, page: DEFAULT_PAGE })}
        />
        {!isBoard && (
          <EnumSelectFilter
            value={params.status}
            onChange={(value) =>
              setParams({
                status: value as typeof params.status,
                page: DEFAULT_PAGE,
              })
            }
            options={roomRentalStatusOptions}
            placeholder="Trạng thái"
            allLabel="Tất cả trạng thái"
          />
        )}
      </FilterBar>

      {isBoard ? (
        <RoomRentalBoard
          search={debouncedSearch || undefined}
          onViewAll={(status) => setParams({ view: "table", status, page: DEFAULT_PAGE })}
        />
      ) : (
        <RoomRentalTable />
      )}
    </div>
  );
};

export default AdminRoomRentalRequestsPage;
