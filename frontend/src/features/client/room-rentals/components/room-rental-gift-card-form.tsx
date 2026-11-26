import { useState, type FormEvent } from "react";
import { CircleAlert, Gift } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { getDisplayMessage, getGiftCardLockSeconds } from "@/lib/error";
import { formatCurrency } from "@/lib/formatters";
import type { OrderGiftCardResponse } from "@/features/client/gift-cards/types";

const GIFT_CARD_CODE_MAX_LENGTH = 24;
const SECONDS_PER_MINUTE = 60;

interface RoomRentalGiftCardFormProps {
  inputId: string;
  cover: OrderGiftCardResponse | null;
  disabled?: boolean;
  onApply: (code: string) => Promise<OrderGiftCardResponse>;
}

export const RoomRentalGiftCardForm = ({
  inputId,
  cover,
  disabled,
  onApply,
}: RoomRentalGiftCardFormProps) => {
  const [code, setCode] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isPending, setIsPending] = useState(false);
  const errorId = `${inputId}-error`;
  const trimmed = code.trim();

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!trimmed || isPending || disabled) return;
    setError(null);
    setIsPending(true);
    try {
      await onApply(trimmed);
      setCode("");
    } catch (cause) {
      const lockSeconds = getGiftCardLockSeconds(cause);
      setError(
        lockSeconds !== null
          ? `Bạn đã nhập sai mã thẻ quá nhiều lần. Thử lại sau ${Math.max(1, Math.ceil(lockSeconds / SECONDS_PER_MINUTE))} phút.`
          : getDisplayMessage(cause, { preferServerMessage: true }),
      );
    } finally {
      setIsPending(false);
    }
  };

  return (
    <section aria-label="Thẻ quà tặng" className="flex flex-col gap-3 border border-border p-3">
      <h4 className="flex items-center gap-2 text-sm font-semibold text-foreground">
        <Gift className="size-4 text-primary-text" aria-hidden />
        Thẻ quà tặng
      </h4>

      {cover && cover.giftCardAmount > 0 && (
        <dl className="flex flex-col gap-1 text-sm">
          <div className="flex justify-between gap-3">
            <dt className="text-muted-foreground">Thẻ quà đã trừ</dt>
            <dd className="tabular font-semibold text-success">
              -{formatCurrency(cover.giftCardAmount)}
            </dd>
          </div>
          <div className="flex justify-between gap-3">
            <dt className="text-muted-foreground">Còn phải thanh toán</dt>
            <dd className="tabular font-semibold text-primary-text">
              {formatCurrency(cover.payableAmount)}
            </dd>
          </div>
        </dl>
      )}

      <form onSubmit={handleSubmit} className="flex flex-col gap-2 sm:flex-row" noValidate>
        <Input
          id={inputId}
          value={code}
          onChange={(event) => {
            setCode(event.target.value);
            if (error) setError(null);
          }}
          placeholder="Nhập mã thẻ quà tặng"
          aria-label="Mã thẻ quà tặng"
          aria-invalid={error ? true : undefined}
          aria-describedby={error ? errorId : undefined}
          autoCapitalize="characters"
          autoComplete="off"
          spellCheck={false}
          maxLength={GIFT_CARD_CODE_MAX_LENGTH}
          disabled={disabled || isPending}
          className="h-11 uppercase placeholder:normal-case"
        />
        <Button
          type="submit"
          variant="outline"
          className="h-11 sm:h-9"
          disabled={disabled || isPending || trimmed.length === 0}
        >
          {isPending ? "Đang áp..." : "Áp dụng"}
        </Button>
      </form>

      {error && (
        <p id={errorId} role="alert" className="flex items-start gap-1.5 text-sm text-destructive">
          <CircleAlert className="mt-0.5 size-4 shrink-0" aria-hidden />
          <span>{error}</span>
        </p>
      )}
    </section>
  );
};
