import { createColumnHelper } from "@tanstack/react-table";
import { MoreHorizontal, Pencil, Trash2 } from "lucide-react";
import { DataTableColumnHeader } from "@/components/common/data-table/data-table-column-header";
import type { DataTableFeatures } from "@/components/common/data-table/data-table-features";
import { ActiveStatusBadge } from "@/components/common/status-badge";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { formatRoomRentalDuration } from "@/features/client/room-rentals/utils/room-rental-labels";
import { formatCurrency } from "@/lib/formatters";
import type { RoomRentalRateDto } from "../types";

interface RoomRentalRateColumnsOptions {
  onEdit: (rate: RoomRentalRateDto) => void;
  onDelete: (rate: RoomRentalRateDto) => void;
}

const columnHelper = createColumnHelper<DataTableFeatures, RoomRentalRateDto>();

export const createRoomRentalRateColumns = ({ onEdit, onDelete }: RoomRentalRateColumnsOptions) =>
  columnHelper.columns([
    columnHelper.accessor("name", {
      header: ({ column }) => <DataTableColumnHeader column={column} title="Bảng giá" />,
      cell: ({ row }) => <span className="font-medium text-foreground">{row.original.name}</span>,
    }),
    columnHelper.accessor("roomName", {
      header: ({ column }) => <DataTableColumnHeader column={column} title="Phòng" />,
    }),
    columnHelper.display({
      id: "window",
      header: "Khung giờ",
      enableSorting: false,
      cell: ({ row }) => (
        <span className="tabular-nums">
          {row.original.startTime} – {row.original.endTime}
        </span>
      ),
    }),
    columnHelper.display({
      id: "duration",
      header: "Thời lượng",
      enableSorting: false,
      cell: ({ row }) => (
        <span className="text-sm">
          {formatRoomRentalDuration(row.original.minDurationMinutes)} –{" "}
          {formatRoomRentalDuration(row.original.maxDurationMinutes)}
        </span>
      ),
    }),
    columnHelper.accessor("basePrice", {
      header: ({ column }) => <DataTableColumnHeader column={column} title="Giá thuê" />,
      cell: ({ row }) => (
        <span className="font-semibold tabular-nums text-primary-text">
          {formatCurrency(row.original.basePrice)}
        </span>
      ),
    }),
    columnHelper.display({
      id: "perPerson",
      header: "Phí đầu người",
      enableSorting: false,
      cell: ({ row }) =>
        row.original.perPersonFee > 0 ? (
          <span className="text-sm tabular-nums">
            {formatCurrency(row.original.perPersonFee)}
            <span className="text-muted-foreground"> / người, miễn phí {row.original.includedAttendees} người đầu</span>
          </span>
        ) : (
          <span className="text-sm text-muted-foreground">Không</span>
        ),
    }),
    columnHelper.accessor("isActive", {
      header: "Trạng thái",
      enableSorting: false,
      cell: ({ row }) => <ActiveStatusBadge active={row.original.isActive} />,
    }),
    columnHelper.display({
      id: "actions",
      header: () => <div className="text-right">Hành động</div>,
      cell: ({ row }) => (
        <div className="flex justify-end">
          <DropdownMenu>
            <DropdownMenuTrigger>
              <Button variant="ghost" size="icon" className="size-8">
                <span className="sr-only">Mở menu</span>
                <MoreHorizontal className="size-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem onClick={() => onEdit(row.original)}>
                <Pencil /> Chỉnh sửa
              </DropdownMenuItem>
              <DropdownMenuItem variant="destructive" onClick={() => onDelete(row.original)}>
                <Trash2 /> Xóa
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      ),
      enableHiding: false,
    }),
  ]);
