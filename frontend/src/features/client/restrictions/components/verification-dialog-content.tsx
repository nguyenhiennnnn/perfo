import { useId, useState } from "react";
import { CheckCircle2 } from "lucide-react";

import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  MS_PER_SECOND,
  VERIFICATION_CODE_LENGTH,
  VERIFICATION_RESEND_COOLDOWN_SECONDS,
} from "@/config/default-constants";
import { useCountdown } from "@/hooks/use-countdown";
import { useDialogClose } from "@/stores/dialog-store";

import {
  useConfirmVerification,
  useSendVerificationCode,
  useVerificationStatus,
} from "../hooks/use-verification";
import { formatRemaining } from "../utils/format-remaining";

export const VerificationDialogContent = () => {
  const inputId = useId();
  const close = useDialogClose();
  const status = useVerificationStatus(true);
  const sendCode = useSendVerificationCode();
  const confirm = useConfirmVerification();
  const [code, setCode] = useState("");

  const data = status.data;
  const codeExpiry = useCountdown(data?.codeExpiresAt ?? null);
  const cooldownEndsAt = data?.codeSentAt
    ? new Date(
        new Date(data.codeSentAt).getTime() + VERIFICATION_RESEND_COOLDOWN_SECONDS * MS_PER_SECOND,
      ).toISOString()
    : null;
  const cooldown = useCountdown(cooldownEndsAt);
  const cooldownSeconds = Math.ceil(cooldown.remainingMs / MS_PER_SECOND);
  const hasActiveCode = codeExpiry.remainingMs > 0;
  const canSend = !sendCode.isPending && cooldownSeconds <= 0;
  const canConfirm = code.length === VERIFICATION_CODE_LENGTH && !confirm.isPending;

  const handleSend = async () => {
    try {
      await sendCode.mutateAsync();
      setCode("");
    } catch {
      void status.refetch();
    }
  };

  const handleConfirm = async () => {
    if (!canConfirm) return;
    try {
      await confirm.mutateAsync(code);
      close();
    } catch {
      setCode("");
      void status.refetch();
    }
  };

  if (status.isLoading) return <CardSkeleton className="h-32" />;

  if (status.isError || !data) {
    return (
      <div className="flex flex-col gap-3">
        <p className="text-sm text-muted-foreground">Không tải được trạng thái xác minh.</p>
        <div className="flex justify-end">
          <Button variant="outline" onClick={() => void status.refetch()}>
            Thử lại
          </Button>
        </div>
      </div>
    );
  }

  if (!data.required) {
    return (
      <div className="flex flex-col gap-4">
        <div className="flex items-start gap-3">
          <CheckCircle2 className="mt-0.5 size-5 shrink-0 text-success" aria-hidden />
          <p className="text-sm text-foreground">
            Tài khoản của bạn đã được xác minh. Bạn có thể tiếp tục đặt vé.
          </p>
        </div>
        <div className="flex justify-end">
          <Button onClick={close}>Đóng</Button>
        </div>
      </div>
    );
  }

  const resendLabel =
    cooldownSeconds > 0
      ? `Gửi lại sau ${cooldownSeconds} giây`
      : hasActiveCode
        ? "Gửi lại mã"
        : "Gửi mã xác minh";

  return (
    <div className="flex flex-col gap-4">
      <p className="text-sm text-foreground">
        Hệ thống phát hiện hoạt động bất thường trên tài khoản của bạn. Để tiếp tục đặt vé, hãy
        xác minh bằng mã gồm {VERIFICATION_CODE_LENGTH} chữ số gửi tới email đăng ký.
      </p>

      {hasActiveCode ? (
        <form
          className="flex flex-col gap-3"
          onSubmit={(event) => {
            event.preventDefault();
            void handleConfirm();
          }}
        >
          <div className="flex flex-col gap-1.5">
            <Label htmlFor={inputId}>Mã xác minh</Label>
            <Input
              id={inputId}
              value={code}
              inputMode="numeric"
              autoComplete="one-time-code"
              maxLength={VERIFICATION_CODE_LENGTH}
              placeholder={"0".repeat(VERIFICATION_CODE_LENGTH)}
              className="t-code tracking-widest"
              onChange={(event) =>
                setCode(event.target.value.replace(/\D/g, "").slice(0, VERIFICATION_CODE_LENGTH))
              }
            />
            <p className="text-xs text-muted-foreground">
              Mã hết hạn sau {formatRemaining(codeExpiry.remainingMs)}
              {typeof data.attemptsLeft === "number" && ` · Còn ${data.attemptsLeft} lần thử`}.
            </p>
          </div>
          <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
            <Button
              type="button"
              variant="outline"
              disabled={!canSend}
              onClick={() => void handleSend()}
            >
              {resendLabel}
            </Button>
            <Button type="submit" disabled={!canConfirm}>
              Xác nhận
            </Button>
          </div>
        </form>
      ) : (
        <div className="flex justify-end">
          <Button disabled={!canSend} onClick={() => void handleSend()}>
            {resendLabel}
          </Button>
        </div>
      )}
    </div>
  );
};
