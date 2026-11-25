import { useState, type FormEvent } from "react";
import { CircleAlert, Gift, Lock, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { MS_PER_SECOND } from "@/config/default-constants";
import { useCountdown } from "@/hooks/use-countdown";
import { getDisplayMessage, getGiftCardLockSeconds } from "@/lib/error";
import { formatCurrency, formatDateTime } from "@/lib/formatters";
import { cn } from "@/lib/utils";
import { GiftCardStatus } from "@/types/enum";
import type {
  GiftCardLookupDto,
  OrderGiftCardItemDto,
} from "@/features/client/gift-cards/types";

const GIFT_CARD_CODE_MAX_LENGTH = 24;
const USABLE_STATUSES: string[] = [GiftCardStatus.Sent, GiftCardStatus.InUse];

type PendingAction = "lookup" | "apply" | { remove: number } | null;

interface GiftCardCodeBoxProps {
  inputId: string;
  title?: string;
  cards: OrderGiftCardItemDto[];
  dueAmount: number;
  onLookup: (code: string) => Promise<GiftCardLookupDto>;
  onApply: (code: string) => Promise<unknown>;
  onRemove: (giftCardId: number) => Promise<unknown>;
  disabled?: boolean;
  disabledHint?: string;
  className?: string;
}

export const GiftCardCodeBox = ({
  inputId,
  title = "Thẻ quà tặng",
  cards,
  dueAmount,
  onLookup,
  onApply,
  onRemove,
  disabled,
  disabledHint,
  className,
}: GiftCardCodeBoxProps) => {
  const [code, setCode] = useState("");
  const [lookup, setLookup] = useState<GiftCardLookupDto | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [lockedUntil, setLockedUntil] = useState<string | null>(null);
  const [pending, setPending] = useState<PendingAction>(null);

  const countdown = useCountdown(lockedUntil);
  const isLocked = lockedUntil !== null && countdown.remainingMs > 0;
  const isBusy = pending !== null;
  const isDisabled = disabled || isBusy || isLocked;
  const errorId = `${inputId}-error`;
  const trimmed = code.trim();
  const canTopUp = dueAmount > 0;

  const handleError = (cause: unknown) => {
    const lockSeconds = getGiftCardLockSeconds(cause);
    if (lockSeconds !== null) {
      setLockedUntil(new Date(Date.now() + lockSeconds * MS_PER_SECOND).toISOString());
      setError(null);
      return;
    }
    setError(getDisplayMessage(cause, { preferServerMessage: true }));
  };

  const run = async (action: PendingAction, task: () => Promise<void>) => {
    setError(null);
    setPending(action);
    try {
      await task();
    } catch (cause) {
      handleError(cause);
    } finally {
      setPending(null);
    }
  };

  const checkBalance = () => {
    if (!trimmed) {
      setError("Nhập mã thẻ quà để kiểm tra.");
      return;
    }
    void run("lookup", async () => {
      setLookup(await onLookup(trimmed));
    });
  };

  const apply = () => {
    if (!trimmed) {
      setError("Nhập mã thẻ quà để áp dụng.");
      return;
    }
    void run("apply", async () => {
      await onApply(trimmed);
      setCode("");
      setLookup(null);
    });
  };

  const remove = (giftCardId: number) =>
    void run({ remove: giftCardId }, async () => {
      await onRemove(giftCardId);
    });

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (isDisabled || !canTopUp) return;
    apply();
  };

  const available = lookup?.availableBalance ?? 0;
  const isLookupUsable =
    lookup !== null &&
    USABLE_STATUSES.includes(lookup.status) &&
    new Date(lookup.expiresAt).getTime() > Date.now();
  const deduction = isLookupUsable ? Math.min(available, dueAmount) : 0;
  const remainder = Math.max(0, dueAmount - deduction);
  const appliedTotal = cards.reduce((sum, card) => sum + card.amount, 0);

  return (
    <section aria-label={title} className={cn("flex flex-col gap-3 bg-card p-4", className)}>
      <h3 className="t-h3 text-foreground">{title}</h3>

      {cards.length > 0 && (
        <ul className="flex flex-col gap-2">
          {cards.map((card) => {
            const isRemoving =
              typeof pending === "object" && pending !== null && pending.remove === card.giftCardId;
            return (
              <li
                key={card.giftCardId}
                data-testid="gift-card-applied"
                className="flex items-center justify-between gap-3 bg-secondary p-3"
              >
                <div className="flex min-w-0 items-center gap-2">
                  <Gift className="size-4 shrink-0 text-success" aria-hidden />
                  <span className="t-code truncate text-sm text-foreground">{card.code}</span>
                </div>
                <div className="flex shrink-0 items-center gap-2">
                  <span className="text-sm font-semibold tabular-nums text-success">
                    -{formatCurrency(card.amount)}
                  </span>
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    disabled={disabled || isBusy}
                    aria-label={`Gỡ thẻ ${card.code}`}
                    onClick={() => remove(card.giftCardId)}
                  >
                    <X />
                    <span>{isRemoving ? "Đang gỡ..." : "Gỡ"}</span>
                  </Button>
                </div>
              </li>
            );
          })}
        </ul>
      )}

      {canTopUp && (
        <form onSubmit={handleSubmit} className="flex flex-col gap-2 sm:flex-row" noValidate>
          <Input
            id={inputId}
            value={code}
            onChange={(event) => {
              setCode(event.target.value);
              setLookup(null);
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
            disabled={isDisabled}
            className="h-11 uppercase placeholder:normal-case"
          />
          <div className="flex gap-2">
            <Button
              type="button"
              variant="outline"
              disabled={isDisabled || trimmed.length === 0}
              onClick={checkBalance}
            >
              {pending === "lookup" ? "Đang kiểm tra..." : "Kiểm tra"}
            </Button>
            <Button type="submit" disabled={isDisabled || trimmed.length === 0}>
              {pending === "apply" ? "Đang áp..." : "Áp dụng"}
            </Button>
          </div>
        </form>
      )}

      {isLocked && (
        <p
          role="alert"
          data-testid="gift-card-locked"
          className="flex items-start gap-1.5 bg-warning/10 p-3 text-sm text-warning-text"
        >
          <Lock className="mt-0.5 size-4 shrink-0" aria-hidden />
          <span>
            Bạn đã nhập sai mã thẻ quá nhiều lần nên tạm bị khóa. Thử lại sau{" "}
            <span className="t-code font-semibold tabular-nums">{countdown.label}</span>.
          </span>
        </p>
      )}

      {error && (
        <p id={errorId} role="alert" className="flex items-start gap-1.5 text-sm text-destructive">
          <CircleAlert className="mt-0.5 size-4 shrink-0" aria-hidden />
          <span>{error}</span>
        </p>
      )}

      {lookup && canTopUp && (
        <dl className="flex flex-col gap-1.5 border border-border p-3 text-sm" data-testid="gift-card-lookup">
          <div className="flex justify-between gap-3">
            <dt className="text-muted-foreground">Thẻ</dt>
            <dd className="t-code text-foreground">{lookup.code}</dd>
          </div>
          <div className="flex justify-between gap-3">
            <dt className="text-muted-foreground">Số dư</dt>
            <dd className="font-semibold tabular-nums text-foreground">
              {formatCurrency(lookup.availableBalance)}
            </dd>
          </div>
          <div className="flex justify-between gap-3">
            <dt className="text-muted-foreground">Hạn dùng</dt>
            <dd className="text-foreground">{formatDateTime(lookup.expiresAt)}</dd>
          </div>
          {isLookupUsable ? (
            <>
              <div className="flex justify-between gap-3">
                <dt className="text-muted-foreground">Số tiền sẽ trừ</dt>
                <dd className="font-semibold tabular-nums text-success">-{formatCurrency(deduction)}</dd>
              </div>
              <div className="flex justify-between gap-3">
                <dt className="text-muted-foreground">Còn phải thanh toán</dt>
                <dd className="font-semibold tabular-nums text-primary-text">{formatCurrency(remainder)}</dd>
              </div>
            </>
          ) : (
            <p className="text-xs text-destructive">
              Thẻ này không dùng được: đã hết hạn, đã dùng hết hoặc chưa được gửi.
            </p>
          )}
        </dl>
      )}

      {cards.length > 0 && (
        <div className="flex flex-col gap-1 border-t border-border pt-3 text-sm">
          <div className="flex justify-between gap-3">
            <span className="text-muted-foreground">Thẻ quà đã trừ</span>
            <span className="font-semibold tabular-nums text-success">-{formatCurrency(appliedTotal)}</span>
          </div>
          <div className="flex justify-between gap-3">
            <span className="text-muted-foreground">Còn phải thanh toán</span>
            <span data-testid="gift-card-remaining" className="font-semibold tabular-nums text-primary-text">
              {formatCurrency(dueAmount)}
            </span>
          </div>
        </div>
      )}

      {disabled && disabledHint && <p className="text-xs text-muted-foreground">{disabledHint}</p>}
    </section>
  );
};
