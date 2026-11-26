import { createColumnHelper } from "@tanstack/react-table";
import { Link } from "react-router";
import { DataTableColumnHeader } from "@/components/common/data-table/data-table-column-header";
import type { DataTableFeatures } from "@/components/common/data-table/data-table-features";
import { ROUTES } from "@/config/routes";
import { ROOM_RENTAL_AUDIENCE_LABELS } from "@/features/client/room-rentals/utils/room-rental-labels";
import { formatDateTime } from "@/lib/formatters";
import type { RoomRentalAdminListItemDto } from "../types";
import { RoomRentalStatusBadge } from "./room-rental-status-badge";

const columnHelper = createColumnHelper<DataTableFeatures, RoomRentalAdminListItemDto>();

export const createRoomRentalColumns = () =>
  columnHelper.columns([
    columnHelper.accessor("requestCode", {
      header: ({ column }) => <DataTableColumnHeader column={column} title="Mã yêu cầu" />,
      cell: ({ row }) => (
        <Link
          to={ROUTES.admin.roomRentalDetail(row.original.roomRentalRequestId)}
          className="t-code text-foreground underline-offset-4 hover:underline"
        >
          {row.original.requestCode}
        </Link>
      ),
    }),
    columnHelper.display({
      id: "customer",
      header: "Khách hàng",
      enableSorting: false,
      cell: ({ row }) => (
        <div className="min-w-0">
          <p className="font-medium text-foreground">{row.original.fullName}</p>
          <p className="truncate text-xs text-muted-foreground">
            {row.original.organizationName ?? ROOM_RENTAL_AUDIENCE_LABELS[row.original.audienceType]}
            {" · "}
            {row.original.contactPhone}
          </p>
        </div>
      ),
    }),
    columnHelper.accessor("desiredStartAt", {
      header: ({ column }) => <DataTableColumnHeader column={column} title="Ngày giờ mong muốn" />,
      cell: ({ row }) => (
        <span className="text-sm tabular-nums">{formatDateTime(row.original.desiredStartAt)}</span>
      ),
    }),
    columnHelper.accessor("attendeeCount", {
      header: ({ column }) => <DataTableColumnHeader column={column} title="Số người" />,
      cell: ({ row }) => <span className="tabular-nums">{row.original.attendeeCount}</span>,
    }),
    columnHelper.accessor("status", {
      header: "Trạng thái",
      enableSorting: false,
      cell: ({ row }) => <RoomRentalStatusBadge status={row.original.status} />,
    }),
    columnHelper.accessor("createdAt", {
      header: ({ column }) => <DataTableColumnHeader column={column} title="Gửi lúc" />,
      cell: ({ row }) => (
        <span className="text-sm text-muted-foreground tabular-nums">
          {formatDateTime(row.original.createdAt)}
        </span>
      ),
    }),
  ]);
