import { useMemo, useState } from "react";
import { Undo2 } from "lucide-react";

import { Badge, type badgeVariants } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Switch } from "@/components/ui/switch";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { SelectField } from "@/components/common/select-field";
import { formatCurrency } from "@/lib/formatters";
import { cn } from "@/lib/utils";
import type { VariantProps } from "class-variance-authority";

import type {
  RoomChangeMatchKind,
  RoomChangePreviewResponse,
  RoomChangeSeatMapping,
} from "../../types";
import {
  MATCH_KIND_LABEL,
  buildTargetOwners,
  describeSeat,
  formatSeatRef,
  getSeatStatusLabel,
  getSelectableTargetSeats,
  isRequiredMapping,
} from "../../utils/room-change";

type BadgeVariant = VariantProps<typeof badgeVariants>["variant"];

const MATCH_KIND_VARIANT: Record<RoomChangeMatchKind, BadgeVariant> = {
  Exact: "secondary",
  Nearest: "warning",
  Manual: "default",
  Unmapped: "destructive",
};

interface RoomChangeMappingTableProps {
  plan: RoomChangePreviewResponse;
  overrides: Record<number, number>;
  isBusy: boolean;
  onAssign: (showtimeSeatId: number, targetSeatId: number) => void;
  onClearOverride: (showtimeSeatId: number) => void;
}

export const RoomChangeMappingTable = ({
  plan,
  overrides,
  isBusy,
  onAssign,
  onClearOverride,
}: RoomChangeMappingTableProps) => {
  const [onlyReview, setOnlyReview] = useState(false);

  const occupied = useMemo(
    () => plan.mappings.filter(isRequiredMapping),
    [plan.mappings],
  );
  const owners = useMemo(() => buildTargetOwners(plan.mappings), [plan.mappings]);

  const isHighlighted = (mapping: RoomChangeSeatMapping) =>
    mapping.needsReview || mapping.showtimeSeatId in overrides;

  const rows = onlyReview ? occupied.filter(isHighlighted) : occupied;
  const hasCouple = occupied.some((mapping) => mapping.source.isCouple);

  if (occupied.length === 0) {
    return (
      <p className="text-sm text-muted-foreground">
        Suất chiếu chưa có ghế nào được đặt hoặc đang giữ, nên không cần ánh xạ ghế.
      </p>
    );
  }

  return (
    <div className="flex flex-col gap-3">
      <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
        <p className="text-sm font-medium text-foreground">
          Ánh xạ ghế ({occupied.length} ghế có khách)
        </p>
        <label className="flex items-center gap-2 text-sm text-muted-foreground">
          <Switch
            size="sm"
            checked={onlyReview}
            onCheckedChange={setOnlyReview}
            aria-label="Chỉ hiện ghế cần chỉnh"
          />
          Chỉ hiện ghế cần chỉnh
        </label>
      </div>

      <div className={cn("max-h-80 overflow-auto", isBusy && "opacity-60")}>
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Ghế hiện tại</TableHead>
              <TableHead>Trạng thái</TableHead>
              <TableHead>Ghế ở {plan.targetRoomName}</TableHead>
              <TableHead>Kiểu ghép</TableHead>
              <TableHead className="text-right">Hoàn chênh lệch</TableHead>
              <TableHead className="w-10" />
            </TableRow>
          </TableHeader>
          <TableBody>
            {rows.length === 0 ? (
              <TableRow>
                <TableCell colSpan={6} className="text-center text-sm text-muted-foreground">
                  Không còn ghế nào cần chỉnh.
                </TableCell>
              </TableRow>
            ) : (
              rows.map((mapping) => {
                const options = getSelectableTargetSeats(
                  plan.targetSeats,
                  owners,
                  mapping.showtimeSeatId,
                );
                const isOverridden = mapping.showtimeSeatId in overrides;
                return (
                  <TableRow key={mapping.showtimeSeatId}>
                    <TableCell>
                      <div className="flex flex-col">
                        <span className="font-medium tabular-nums text-foreground">
                          {formatSeatRef(mapping.source)}
                        </span>
                        {mapping.source.seatType && (
                          <span className="text-xs text-muted-foreground">
                            {mapping.source.seatType}
                            {mapping.source.isCouple ? " (ghế đôi)" : ""}
                          </span>
                        )}
                      </div>
                    </TableCell>
                    <TableCell>
                      <Badge variant={mapping.status === "Booked" ? "success" : "secondary"}>
                        {getSeatStatusLabel(mapping.status)}
                      </Badge>
                    </TableCell>
                    <TableCell className="min-w-44">
                      <SelectField
                        value={mapping.targetSeatId ?? null}
                        onChange={(value) => onAssign(mapping.showtimeSeatId, Number(value))}
                        options={options}
                        getOptionValue={(seat) => seat.seatId}
                        getOptionLabel={(seat) =>
                          seat.isCouple ? `${describeSeat(seat)} (đôi)` : describeSeat(seat)
                        }
                        placeholder="Chọn ghế"
                        disabled={isBusy}
                      />
                    </TableCell>
                    <TableCell>
                      <Badge variant={MATCH_KIND_VARIANT[mapping.matchKind]}>
                        {MATCH_KIND_LABEL[mapping.matchKind]}
                      </Badge>
                    </TableCell>
                    <TableCell className="text-right tabular-nums">
                      {mapping.refundDue > 0 ? formatCurrency(mapping.refundDue) : "—"}
                    </TableCell>
                    <TableCell>
                      {isOverridden && (
                        <Button
                          variant="ghost"
                          size="icon"
                          className="size-8"
                          disabled={isBusy}
                          onClick={() => onClearOverride(mapping.showtimeSeatId)}
                        >
                          <span className="sr-only">Bỏ chọn tay</span>
                          <Undo2 className="size-4" />
                        </Button>
                      )}
                    </TableCell>
                  </TableRow>
                );
              })
            )}
          </TableBody>
        </Table>
      </div>

      {hasCouple && (
        <p className="text-xs text-muted-foreground">
          Ghế đôi phải chuyển sang đúng một cặp ghế đôi ở phòng mới. Khi chỉnh một ghế của cặp,
          hãy chỉnh cả ghế còn lại.
        </p>
      )}
    </div>
  );
};
