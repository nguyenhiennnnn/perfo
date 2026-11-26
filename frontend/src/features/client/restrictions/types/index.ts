export interface CustomerVerificationStatusDto {
  required: boolean;
  reason?: string | null;
  codeSentAt?: string | null;
  codeExpiresAt?: string | null;
  attemptsLeft?: number | null;
}

export interface ConfirmVerificationRequest {
  code: string;
}
