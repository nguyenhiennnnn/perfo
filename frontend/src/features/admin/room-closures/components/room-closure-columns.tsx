import { createColumnHelper } from "@tanstack/react-table";
import { CalendarX2, ListChecks, MoreHorizontal, Undo2 } from "lucide-react";

import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { DataTableColumnHeader } from "@/components/common/data-table/data-table-column-header";
import type { DataTableFeatures } from "@/components/common/data-table/data-table-features";
import { formatDateTime } from "@/lib/formatters";

import type { RoomClosureDto } from "../types";
import { getClosureStatus } from "../utils/closure-status";
import { RoomClosureStatusBadge } from "./room-closure-status-badge";

interface RoomClosureColumnsOptions {
  onHandle: (closure: RoomClosureDto) => void;
  onEnd: (closure: RoomClosureDto) => void;
}

const columnHelper = createColumnHelper<DataTableFeatures, RoomClosureDto>();

export const createRoomClosureColumns = ({
  onHandle,
  onEnd,
}: RoomClosureColumnsOptions) =>
  columnHelper.columns([
    columnHelper.accessor("roomName", {
      header: ({ column }) => <DataTableColumnHeader column={column} title="Phòng chiếu" />,
      cell: ({ row }) => (
        <span className="font-medium text-foreground">{row.original.roomName}</span>
      ),
      enableSorting: false,
    }),
    columnHelper.display({
      id: "period",
      header: () => <span>Thời gian đóng</span>,
      cell: ({ row }) => (
        <div className="flex flex-col text-sm tabular-nums">
          <span className="text-foreground">{formatDateTime(row.original.startTime)}</span>
          <span className="text-muted-foreground">đến {formatDateTime(row.original.endTime)}</span>
        </div>
      ),
    }),
    columnHelper.accessor("reason", {
      header: () => <span>Lý do</span>,
      cell: ({ row }) => (
        <span className="line-clamp-2 max-w-xs text-sm text-foreground" title={row.original.reason}>
          {row.original.reason}
        </span>
      ),
      enableSorting: false,
    }),
    columnHelper.display({
      id: "status",
      header: () => <span>Trạng thái</span>,
      cell: ({ row }) => <RoomClosureStatusBadge status={getClosureStatus(row.original)} />,
    }),
    columnHelper.display({
      id: "actions",
      header: () => <div className="text-right">Hành động</div>,
      cell: ({ row }) => {
        const status = getClosureStatus(row.original);
        return (
          <div className="flex justify-end">
            <DropdownMenu>
              <DropdownMenuTrigger>
                <Button variant="ghost" size="icon" className="size-8">
                  <span className="sr-only">Mở menu</span>
                  <MoreHorizontal className="size-4" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                <DropdownMenuItem onClick={() => onHandle(row.original)}>
                  <ListChecks /> Xử lý suất bị ảnh hưởng
                </DropdownMenuItem>
                {status !== "ended" && (
                  <DropdownMenuItem variant="destructive" onClick={() => onEnd(row.original)}>
                    {status === "upcoming" ? <CalendarX2 /> : <Undo2 />}
                    {status === "upcoming" ? "Hủy lịch đóng" : "Mở lại phòng sớm"}
                  </DropdownMenuItem>
                )}
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        );
      },
      enableHiding: false,
    }),
  ]);
