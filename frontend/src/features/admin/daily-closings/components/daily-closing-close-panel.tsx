import { useState } from "react";
import { Lock, TriangleAlert } from "lucide-react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Switch } from "@/components/ui/switch";
import { Textarea } from "@/components/ui/textarea";
import { FORM_PROCESSING_LABEL } from "@/config/default-constants";
import { formatCurrency, formatDate } from "@/lib/formatters";
import { cn } from "@/lib/utils";
import { useAlertDialogStore } from "@/stores/alert-dialog-store";
import { parseMoneyInput } from "@/features/admin/counter/utils/cash";

import { useAdminCloseDailyClosing } from "../hooks/use-admin-daily-closings";
import { getClosingBlockers } from "../utils/closing-blockers";
import {
  DAILY_CLOSING_MAX_CASH,
  DAILY_CLOSING_NOTE_MAX_LENGTH,
  type DailyClosingSnapshot,
} from "../types";

interface DailyClosingClosePanelProps {
  date: string;
  snapshot: DailyClosingSnapshot;
  dayNotEnded: boolean;
  allowOverride: boolean;
}

export const DailyClosingClosePanel = ({
  date,
  snapshot,
  dayNotEnded,
  allowOverride,
}: DailyClosingClosePanelProps) => {
  const openAlert = useAlertDialogStore((state) => state.open);
  const closeDay = useAdminCloseDailyClosing();

  const [countedCash, setCountedCash] = useState(snapshot.cash.expectedCash);
  const [note, setNote] = useState("");
  const [override, setOverride] = useState(false);

  const blockers = getClosingBlockers(snapshot, dayNotEnded);
  const hasHardBlocker = blockers.some((b) => !b.overridable);
  const hasBlocker = blockers.length > 0;
  const overrideActive = hasBlocker && !hasHardBlocker && allowOverride && override;

  const variance = countedCash - snapshot.cash.expectedCash;
  const trimmedNote = note.trim();
  const isCashInvalid = countedCash < 0 || countedCash > DAILY_CLOSING_MAX_CASH;
  const isNoteTooLong = note.length > DAILY_CLOSING_NOTE_MAX_LENGTH;
  const needsNote = overrideActive && trimmedNote.length === 0;
  const blocked = hasBlocker && !overrideActive;
  const canSubmit =
    !blocked && !isCashInvalid && !isNoteTooLong && !needsNote && !closeDay.isPending;

  const handleClose = () => {
    if (!canSubmit) return;
    openAlert({
      title: `Chốt doanh thu ngày ${formatDate(date)}`,
      description:
        "Sau khi chốt, số liệu của ngày bị khóa và không thể sửa trực tiếp. Mọi điều chỉnh về sau phải tạo bút toán.",
      confirmText: "Chốt ngày",
      onConfirm: async () => {
        await closeDay.mutateAsync({
          date,
          countedCash,
          note: trimmedNote || null,
          overrideBlockers: overrideActive,
        });
      },
    });
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>Chốt ngày</CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        {hasBlocker ? (
          <div role="alert" className="space-y-2 bg-warning/10 p-3 text-sm text-foreground">
            <p className="flex items-center gap-2 font-medium">
              <TriangleAlert className="size-4 shrink-0 text-warning" />
              Chưa đủ điều kiện để chốt ngày
            </p>
            <ul className="list-disc space-y-1 pl-6">
              {blockers.map((blocker) => (
                <li key={blocker.key}>{blocker.message}</li>
              ))}
            </ul>
          </div>
        ) : null}

        {snapshot.orders.pendingOrders > 0 ? (
          <p className="bg-muted p-3 text-sm text-muted-foreground">
            Còn {snapshot.orders.pendingOrders} đơn đang chờ thanh toán. Đơn này không chặn việc chốt
            ngày nhưng sẽ không nằm trong doanh thu.
          </p>
        ) : null}

        {hasBlocker && !hasHardBlocker ? (
          allowOverride ? (
            <div className="flex items-start justify-between gap-4 bg-card p-3">
              <div className="space-y-0.5">
                <Label htmlFor="closing-override">Bỏ qua điều kiện chưa đủ</Label>
                <p className="text-xs text-muted-foreground">
                  Bắt buộc ghi chú lý do. Việc bỏ qua được ghi vào nhật ký.
                </p>
              </div>
              <Switch id="closing-override" checked={override} onCheckedChange={setOverride} />
            </div>
          ) : (
            <p className="text-xs text-muted-foreground">
              Cài đặt hệ thống hiện không cho phép chốt ngày khi điều kiện chưa đủ.
            </p>
          )
        ) : null}

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="closing-counted-cash">Tổng tiền mặt thực đếm cả ngày</Label>
          <Input
            id="closing-counted-cash"
            type="number"
            inputMode="numeric"
            min={0}
            max={DAILY_CLOSING_MAX_CASH}
            step={1000}
            value={countedCash}
            aria-invalid={isCashInvalid || undefined}
            onChange={(e) => setCountedCash(parseMoneyInput(e.target.value))}
          />
          <p className="text-xs text-muted-foreground">
            Phải có theo sổ sách (tổng các ca): {formatCurrency(snapshot.cash.expectedCash)}
          </p>
          {isCashInvalid ? (
            <p className="text-xs text-destructive">
              Số tiền không được âm và không vượt quá {formatCurrency(DAILY_CLOSING_MAX_CASH)}.
            </p>
          ) : (
            <p
              className={cn(
                "text-xs font-medium tabular-nums",
                variance === 0 ? "text-success" : "text-warning-text",
              )}
            >
              Chênh lệch: {formatCurrency(variance)}
              {variance === 0 ? " (khớp sổ sách)" : variance > 0 ? " (thừa)" : " (thiếu)"}
            </p>
          )}
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="closing-note">
            Ghi chú{overrideActive ? " (bắt buộc)" : variance !== 0 ? " (nên giải thích chênh lệch)" : ""}
          </Label>
          <Textarea
            id="closing-note"
            rows={3}
            className="min-h-20 resize-none"
            value={note}
            aria-invalid={isNoteTooLong || needsNote || undefined}
            onChange={(e) => setNote(e.target.value)}
          />
          <p className={cn("text-xs", isNoteTooLong ? "text-destructive" : "text-muted-foreground")}>
            {note.length}/{DAILY_CLOSING_NOTE_MAX_LENGTH}
          </p>
        </div>

        <Button onClick={handleClose} disabled={!canSubmit} className="w-full sm:w-auto">
          <Lock className="size-4" />
          <span>{closeDay.isPending ? FORM_PROCESSING_LABEL : "Chốt ngày"}</span>
        </Button>
      </CardContent>
    </Card>
  );
};
