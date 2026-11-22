import { HTTP_STATUS } from "@/config/http-status";
import { extractApiError } from "@/lib/error";
import type { CheckinFailure, CheckinResponse } from "../types";

const FALLBACK_MESSAGE = "Không soát được vé. Kiểm tra mã vé rồi thử lại.";

export function classifyCheckinError(
  error: unknown,
  previous?: CheckinResponse | null,
): CheckinFailure {
  const { statusCode, message } = extractApiError(error);
  const text = message || FALLBACK_MESSAGE;

  if (statusCode === HTTP_STATUS.conflict)
    return { kind: "duplicate", message: text, previous: previous ?? null };
  if (statusCode === HTTP_STATUS.notFound)
    return { kind: "notFound", message: text };
  if (
    statusCode === HTTP_STATUS.badRequest ||
    statusCode === HTTP_STATUS.unprocessable
  )
    return { kind: "invalid", message: text };
  return { kind: "unknown", message: text };
}
