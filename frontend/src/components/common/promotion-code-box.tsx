import { useState, type FormEvent } from "react";
import { CircleAlert, Tag, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { getDisplayMessage } from "@/lib/error";
import { formatCurrency } from "@/lib/formatters";
import { cn } from "@/lib/utils";

const PROMOTION_CODE_MAX_LENGTH = 30;

export interface PromotionSuggestion {
  code: string;
  name: string;
  benefit: string;
  condition?: string;
}

interface PromotionCodeBoxProps {
  inputId: string;
  title?: string;
  appliedCode?: string | null;
  appliedName?: string | null;
  discountAmount?: number;
  onApply: (code: string) => Promise<unknown>;
  onRemove: () => Promise<unknown>;
  disabled?: boolean;
  disabledHint?: string;
  suggestions?: PromotionSuggestion[];
  suggestionsLoading?: boolean;
  className?: string;
}

type PendingAction = { type: "apply"; code: string } | { type: "remove" } | null;

export const PromotionCodeBox = ({
  inputId,
  title = "Mã khuyến mãi",
  appliedCode,
  appliedName,
  discountAmount = 0,
  onApply,
  onRemove,
  disabled,
  disabledHint,
  suggestions = [],
  suggestionsLoading,
  className,
}: PromotionCodeBoxProps) => {
  const [code, setCode] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [pending, setPending] = useState<PendingAction>(null);

  const isBusy = pending !== null;
  const isLocked = disabled || isBusy;
  const errorId = `${inputId}-error`;

  const apply = async (rawCode: string) => {
    const value = rawCode.trim();
    if (!value) {
      setError("Nhập mã khuyến mãi để áp dụng.");
      return;
    }
    setError(null);
    setPending({ type: "apply", code: value });
    try {
      await onApply(value);
      setCode("");
    } catch (applyError) {
      setError(getDisplayMessage(applyError, { preferServerMessage: true }));
    } finally {
      setPending(null);
    }
  };

  const remove = async () => {
    setError(null);
    setPending({ type: "remove" });
    try {
      await onRemove();
    } catch (removeError) {
      setError(getDisplayMessage(removeError, { preferServerMessage: true }));
    } finally {
      setPending(null);
    }
  };

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (isLocked) return;
    void apply(code);
  };

  const hasApplied = Boolean(appliedCode);
  const showSuggestions = !hasApplied && (suggestionsLoading || suggestions.length > 0);
  const applyingCode = pending?.type === "apply" ? pending.code : null;

  return (
    <section aria-label={title} className={cn("flex flex-col gap-3 bg-card p-4", className)}>
      <h3 className="t-h3 text-foreground">{title}</h3>

      {hasApplied ? (
        <div className="flex items-center justify-between gap-3 bg-secondary p-3">
          <div className="flex min-w-0 items-center gap-2">
            <Tag className="size-4 shrink-0 text-success" aria-hidden />
            <div className="flex min-w-0 flex-col">
              <span data-testid="promotion-applied-code" className="t-code text-sm text-foreground">
                {appliedCode}
              </span>
              {appliedName && (
                <span className="truncate text-xs text-muted-foreground">{appliedName}</span>
              )}
            </div>
          </div>
          <div className="flex shrink-0 items-center gap-2">
            {discountAmount > 0 && (
              <span
                data-testid="promotion-applied-discount"
                className="text-sm font-semibold tabular-nums text-success"
              >
                -{formatCurrency(discountAmount)}
              </span>
            )}
            <Button
              type="button"
              variant="outline"
              size="sm"
              disabled={isLocked}
              aria-label={`Gỡ mã ${appliedCode}`}
              onClick={() => void remove()}
            >
              <X />
              <span>{pending?.type === "remove" ? "Đang gỡ..." : "Gỡ"}</span>
            </Button>
          </div>
        </div>
      ) : (
        <form onSubmit={handleSubmit} className="flex gap-2" noValidate>
          <Input
            id={inputId}
            value={code}
            onChange={(event) => {
              setCode(event.target.value);
              if (error) setError(null);
            }}
            placeholder="Nhập mã khuyến mãi"
            aria-label="Mã khuyến mãi"
            aria-invalid={error ? true : undefined}
            aria-describedby={error ? errorId : undefined}
            autoCapitalize="characters"
            autoComplete="off"
            spellCheck={false}
            maxLength={PROMOTION_CODE_MAX_LENGTH}
            disabled={isLocked}
            className="h-11 uppercase placeholder:normal-case"
          />
          <Button type="submit" disabled={isLocked || code.trim().length === 0}>
            {applyingCode !== null && applyingCode === code.trim() ? "Đang áp..." : "Áp dụng"}
          </Button>
        </form>
      )}

      {error && (
        <p
          id={errorId}
          role="alert"
          className="flex items-start gap-1.5 text-sm text-destructive"
        >
          <CircleAlert className="mt-0.5 size-4 shrink-0" aria-hidden />
          <span>{error}</span>
        </p>
      )}

      {disabled && disabledHint && (
        <p className="text-xs text-muted-foreground">{disabledHint}</p>
      )}

      {showSuggestions && (
        <div className="flex flex-col gap-2">
          <p className="text-xs font-medium text-muted-foreground">Mã của bạn</p>
          {suggestionsLoading ? (
            <Skeleton className="h-14 w-full" />
          ) : (
            suggestions.map((suggestion) => (
              <Button
                key={suggestion.code}
                type="button"
                variant="outline"
                disabled={isLocked}
                data-testid="promotion-suggestion"
                className="h-auto w-full justify-between gap-3 py-2.5 text-left whitespace-normal"
                onClick={() => void apply(suggestion.code)}
              >
                <span className="flex min-w-0 flex-col gap-0.5">
                  <span className="t-code text-sm">{suggestion.code}</span>
                  <span className="text-xs text-muted-foreground">
                    {suggestion.name} · {suggestion.benefit}
                  </span>
                  {suggestion.condition && (
                    <span className="text-xs text-muted-foreground">{suggestion.condition}</span>
                  )}
                </span>
                <span className="shrink-0 text-xs font-medium text-primary-text">
                  {applyingCode === suggestion.code ? "Đang áp..." : "Áp dụng"}
                </span>
              </Button>
            ))
          )}
        </div>
      )}
    </section>
  );
};
