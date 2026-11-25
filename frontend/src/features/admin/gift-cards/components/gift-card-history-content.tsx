import { History } from "lucide-react";
import { Link } from "react-router";

import { EmptyState } from "@/components/common/empty-state";
import { QueryState } from "@/components/common/query-state";
import { TableRowsSkeleton } from "@/components/common/skeleton-shapes";
import { GiftCardStatusBadge } from "@/components/common/status-badge";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { ROUTES } from "@/config/routes";
import { SignedAmount } from "@/features/admin/ledger-adjustments/components/signed-amount";
import { formatCurrency, formatDateTime } from "@/lib/formatters";
import { useDialogClose } from "@/stores/dialog-store";

import { useAdminGiftCardHistory } from "../hooks/use-admin-gift-cards";
import type { AdminGiftCard } from "../types";
import { getGiftCardLedgerTypeLabel } from "../utils/labels";

interface GiftCardHistoryContentProps {
  giftCard: AdminGiftCard;
}

export const GiftCardHistoryContent = ({ giftCard }: GiftCardHistoryContentProps) => {
  const closeDialog = useDialogClose();
  const history = useAdminGiftCardHistory(giftCard.giftCardId);

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-wrap items-center gap-x-6 gap-y-2 text-sm text-muted-foreground">
        <span className="t-code font-medium text-foreground">{giftCard.maskedCode}</span>
        <GiftCardStatusBadge status={giftCard.status} />
        <span>
          Mệnh giá{" "}
          <span className="tabular-nums text-foreground">{formatCurrency(giftCard.faceValue)}</span>
        </span>
        <span>
          Số dư{" "}
          <span className="tabular-nums text-foreground">{formatCurrency(giftCard.balance)}</span>
        </span>
      </div>

      <QueryState
        query={history}
        errorTitle="Không tải được lịch sử thẻ"
        skeleton={
          <Table>
            <TableBody>
              <TableRowsSkeleton rows={4} columns={6} />
            </TableBody>
          </Table>
        }
        isEmpty={(entries) => entries.length === 0}
        empty={
          <EmptyState
            icon={History}
            title="Chưa có lịch sử"
            description="Thẻ này chưa phát sinh giao dịch nào."
          />
        }
      >
        {(entries) => (
          <div className="overflow-x-auto">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Thời điểm</TableHead>
                  <TableHead>Loại</TableHead>
                  <TableHead className="text-right">Số dư thay đổi</TableHead>
                  <TableHead className="text-right">Giữ chỗ thay đổi</TableHead>
                  <TableHead className="text-right">Số dư sau</TableHead>
                  <TableHead>Đơn hàng</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {entries.map((entry) => (
                  <TableRow key={entry.giftCardLedgerId}>
                    <TableCell className="text-sm tabular-nums">
                      {formatDateTime(entry.occurredAt)}
                    </TableCell>
                    <TableCell className="text-sm">
                      {getGiftCardLedgerTypeLabel(entry.entryType)}
                    </TableCell>
                    <TableCell className="text-right text-sm">
                      <SignedAmount value={entry.balanceChange} />
                    </TableCell>
                    <TableCell className="text-right text-sm">
                      <SignedAmount value={entry.reservedChange} />
                    </TableCell>
                    <TableCell className="text-right text-sm tabular-nums">
                      {formatCurrency(entry.balanceAfter)}
                    </TableCell>
                    <TableCell className="text-sm">
                      {entry.orderId ? (
                        <Link
                          to={ROUTES.admin.orderDetail(entry.orderId)}
                          onClick={closeDialog}
                          className="text-primary-text underline-offset-4 hover:underline"
                        >
                          Đơn #{entry.orderId}
                        </Link>
                      ) : (
                        <span className="text-muted-foreground">—</span>
                      )}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        )}
      </QueryState>
    </div>
  );
};
