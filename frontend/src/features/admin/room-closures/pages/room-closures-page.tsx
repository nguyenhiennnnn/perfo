import { DoorClosed, Plus } from "lucide-react";
import { useNavigate } from "react-router";

import { PageHeader } from "@/components/common/page-header";
import { DataTable } from "@/components/common/data-table/data-table";
import { EnumSelectFilter } from "@/components/common/filter/enum-select-filter";
import { Button } from "@/components/ui/button";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { DEFAULT_PAGE, LOOKUP_PAGE_SIZE } from "@/config/default-constants";
import { ROUTES } from "@/config/routes";
import { useAdminRooms } from "@/features/admin/rooms/hooks/use-admin-rooms";
import { useAdminRoomClosureParams } from "@/hooks/use-query-params";
import { useTablePagination } from "@/hooks/use-table-pagination";
import { useDialogClose, useDialogOpen } from "@/stores/dialog-store";

import { createRoomClosureColumns } from "../components/room-closure-columns";
import { RoomClosureForm } from "../components/forms/room-closure-form";
import { useRoomClosureEndFlow } from "../hooks/use-room-closure-end-flow";
import { useRoomClosures } from "../hooks/use-room-closures";
import type { RoomClosureDto } from "../types";

const SCOPE_TABS = [
  { value: "active", label: "Đang và sắp tới" },
  { value: "ended", label: "Đã kết thúc" },
  { value: "all", label: "Tất cả" },
] as const;

const SCOPE_ONGOING = {
  active: true,
  ended: false,
  all: undefined,
} as const;

const AdminRoomClosuresPage = () => {
  const [params, setParams] = useAdminRoomClosureParams();
  const { pagination, onPaginationChange } = useTablePagination(params, setParams);
  const navigate = useNavigate();

  const openDialog = useDialogOpen();
  const closeDialog = useDialogClose();
  const endFlow = useRoomClosureEndFlow();

  const rooms = useAdminRooms({ pageSize: LOOKUP_PAGE_SIZE });
  const closures = useRoomClosures({
    roomId: params.roomId ?? undefined,
    ongoing: SCOPE_ONGOING[params.scope],
    page: params.page,
    pageSize: params.pageSize,
  });

  const roomOptions = (rooms.data?.items ?? []).map((room) => ({
    value: String(room.roomId),
    label: room.roomName,
  }));

  const handleCreate = () => {
    openDialog({
      title: "Đóng phòng chiếu",
      description: "Chọn phòng, khoảng thời gian và lý do. Hệ thống không cho tạo thêm suất trong khoảng này.",
      size: "lg",
      content: (
        <RoomClosureForm
          onCancel={closeDialog}
          onCreated={(closure) => {
            closeDialog();
            if (closure) navigate(ROUTES.admin.roomClosureDetail(closure.roomClosureId));
          }}
        />
      ),
    });
  };

  const handleHandle = (closure: RoomClosureDto) => {
    navigate(ROUTES.admin.roomClosureDetail(closure.roomClosureId));
  };

  const columns = createRoomClosureColumns({
    onHandle: handleHandle,
    onEnd: endFlow.start,
  });

  const hasActiveFilters = params.roomId !== null;

  return (
    <div>
      <PageHeader
        title="Đóng phòng bảo trì"
        description="Đặt phòng chiếu vào trạng thái đóng và xử lý các suất chiếu bị ảnh hưởng."
        action={
          <Button onClick={handleCreate}>
            <Plus className="size-4" />
            <span>Đóng phòng</span>
          </Button>
        }
      />

      <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <Tabs
          value={params.scope}
          onValueChange={(scope) =>
            setParams({
              scope: scope as (typeof SCOPE_TABS)[number]["value"],
              page: DEFAULT_PAGE,
            })
          }
        >
          <TabsList>
            {SCOPE_TABS.map((tab) => (
              <TabsTrigger key={tab.value} value={tab.value}>
                {tab.label}
              </TabsTrigger>
            ))}
          </TabsList>
        </Tabs>

        <EnumSelectFilter
          value={params.roomId !== null ? String(params.roomId) : null}
          onChange={(value) =>
            setParams({ roomId: value === null ? null : Number(value), page: DEFAULT_PAGE })
          }
          options={roomOptions}
          placeholder="Lọc theo phòng chiếu"
          allLabel="Tất cả phòng"
        />
      </div>

      <DataTable
        columns={columns}
        data={closures.data?.items ?? []}
        isLoading={closures.isLoading}
        isError={closures.isError}
        error={closures.error}
        onRetry={() => void closures.refetch()}
        isRefreshing={closures.isPlaceholderData}
        totalItems={closures.data?.totalItems ?? 0}
        pagination={pagination}
        onPaginationChange={onPaginationChange}
        empty={{
          icon: DoorClosed,
          title: "Chưa có lịch đóng phòng",
          description: "Khi phòng hỏng hoặc cần bảo trì, đặt lịch đóng để xử lý các suất bị ảnh hưởng.",
          actionLabel: "Đóng phòng",
          onAction: handleCreate,
        }}
        hasActiveFilters={hasActiveFilters}
        onClearFilters={() => setParams({ roomId: null, page: DEFAULT_PAGE })}
        getRowId={(row) => String(row.roomClosureId)}
      />
    </div>
  );
};

export default AdminRoomClosuresPage;
