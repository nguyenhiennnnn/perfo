import { createColumnHelper } from "@tanstack/react-table";
import { Ban, History, MailCheck, MoreHorizontal } from "lucide-react";

import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { GiftCardStatusBadge } from "@/components/common/status-badge";
import type { DataTableFeatures } from "@/components/common/data-table/data-table-features";
import { STATUS_LABEL } from "@/config/default-constants";
import { formatCurrency, formatDate, formatDateTime } from "@/lib/formatters";

import type { AdminGiftCard } from "../types";
import {
  getGiftCardTypeLabel,
  isGiftCardCancellable,
  isGiftCardResendable,
} from "../utils/labels";

interface GiftCardColumnsOptions {
  onViewHistory: (giftCard: AdminGiftCard) => void;
  onResend: (giftCard: AdminGiftCard) => void;
  onCancel: (giftCard: AdminGiftCard) => void;
}

const columnHelper = createColumnHelper<DataTableFeatures, AdminGiftCard>();

const PersonCell = ({ name, email }: { name: string | null; email: string | null }) => {
  if (!name && !email) return <span className="text-sm text-muted-foreground">—</span>;
  return (
    <div className="flex max-w-56 flex-col gap-0.5">
      <span className="truncate text-sm font-medium text-foreground">{name ?? email}</span>
      {name && email ? (
        <span className="truncate text-xs text-muted-foreground">{email}</span>
      ) : null}
    </div>
  );
};

export const createGiftCardColumns = ({
  onViewHistory,
  onResend,
  onCancel,
}: GiftCardColumnsOptions) =>
  columnHelper.columns([
    columnHelper.accessor("maskedCode", {
      header: "Mã thẻ",
      cell: ({ row }) => (
        <div className="flex flex-col gap-0.5">
          <span className="t-code font-medium">{row.original.maskedCode}</span>
          <span className="text-xs text-muted-foreground">
            {getGiftCardTypeLabel(row.original.type)}
          </span>
        </div>
      ),
      enableSorting: false,
    }),
    columnHelper.display({
      id: "purchaser",
      header: "Người mua",
      cell: ({ row }) => (
        <PersonCell name={row.original.purchaserName} email={row.original.purchaserEmail} />
      ),
      enableSorting: false,
    }),
    columnHelper.display({
      id: "recipient",
      header: "Người nhận",
      cell: ({ row }) => (
        <PersonCell name={row.original.recipientName} email={row.original.recipientEmail} />
      ),
      enableSorting: false,
    }),
    columnHelper.accessor("faceValue", {
      header: () => <div className="text-right">Mệnh giá</div>,
      cell: ({ row }) => (
        <div className="text-right text-sm tabular-nums">{formatCurrency(row.original.faceValue)}</div>
      ),
      enableSorting: false,
    }),
    columnHelper.accessor("balance", {
      header: () => <div className="text-right">Số dư</div>,
      cell: ({ row }) => (
        <div className="flex flex-col items-end gap-0.5">
          <span className="text-sm font-semibold tabular-nums text-primary-text">
            {formatCurrency(row.original.balance)}
          </span>
          {row.original.reservedAmount > 0 ? (
            <span className="text-xs tabular-nums text-muted-foreground">
              Đang giữ {formatCurrency(row.original.reservedAmount)}
            </span>
          ) : null}
        </div>
      ),
      enableSorting: false,
    }),
    columnHelper.display({
      id: "dates",
      header: "Gửi và hạn dùng",
      cell: ({ row }) => {
        const { type, sendAt, expiresAt } = row.original;
        return (
          <div className="flex flex-col gap-0.5 text-xs tabular-nums text-muted-foreground">
            {type === "Gift" ? (
              <span>{sendAt ? `Gửi ${formatDateTime(sendAt)}` : "Gửi ngay khi thanh toán"}</span>
            ) : null}
            <span>Hạn {formatDate(expiresAt)}</span>
          </div>
        );
      },
      enableSorting: false,
    }),
    columnHelper.accessor("status", {
      header: () => STATUS_LABEL,
      cell: ({ row }) => <GiftCardStatusBadge status={row.original.status} />,
      enableSorting: false,
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
              <DropdownMenuItem onClick={() => onViewHistory(row.original)}>
                <History /> Lịch sử sử dụng
              </DropdownMenuItem>
              <DropdownMenuItem
                disabled={!isGiftCardResendable(row.original)}
                onClick={() => onResend(row.original)}
              >
                <MailCheck /> Gửi lại email
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuItem
                variant="destructive"
                disabled={!isGiftCardCancellable(row.original)}
                onClick={() => onCancel(row.original)}
              >
                <Ban /> Hủy thẻ
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      ),
      enableHiding: false,
    }),
  ]);
