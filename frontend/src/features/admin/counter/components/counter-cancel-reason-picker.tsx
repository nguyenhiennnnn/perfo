import { useState } from "react";
import { Chip, ChipGroup } from "@/components/common/chip";
import {
  useAlertDialogLoading,
  useAlertDialogSetConfirmDisabled,
} from "@/stores/alert-dialog-store";
import type { CancelReason } from "@/types/enum";
import {
  COUNTER_CANCEL_REASONS,
  getCancelReasonLabel,
} from "../utils/cancel-order";

interface CounterCancelReasonPickerProps {
  onReasonChange: (reason: CancelReason) => void;
}

export const CounterCancelReasonPicker = ({
  onReasonChange,
}: CounterCancelReasonPickerProps) => {
  const [reason, setReason] = useState<CancelReason | null>(null);
  const setConfirmDisabled = useAlertDialogSetConfirmDisabled();
  const isLoading = useAlertDialogLoading();

  const handleSelect = (item: CancelReason) => {
    setReason(item);
    onReasonChange(item);
    setConfirmDisabled(false);
  };

  return (
    <div className="flex flex-col gap-2">
      <p
        id="counter-cancel-reason-label"
        className="text-sm font-medium text-foreground"
      >
        Lý do hủy <span className="text-destructive">*</span>
      </p>
      <ChipGroup
        role="radiogroup"
        aria-labelledby="counter-cancel-reason-label"
      >
        {COUNTER_CANCEL_REASONS.map((item) => (
          <Chip
            key={item}
            role="radio"
            selected={reason === item}
            disabled={isLoading}
            onClick={() => handleSelect(item)}
          >
            {getCancelReasonLabel(item)}
          </Chip>
        ))}
      </ChipGroup>
      {reason === null && (
        <p className="text-xs text-muted-foreground">
          Chọn lý do để có thể xác nhận hủy đơn.
        </p>
      )}
    </div>
  );
};
