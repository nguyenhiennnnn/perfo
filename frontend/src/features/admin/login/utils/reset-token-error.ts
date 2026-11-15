import { extractApiError, getFieldErrors } from "@/lib/error";
import { HTTP_STATUS } from "@/config/http-status";

export function isInvalidResetTokenError(error: unknown): boolean {
  if (extractApiError(error).statusCode !== HTTP_STATUS.badRequest) return false;
  return Object.keys(getFieldErrors(error)).every((field) => field === "token");
}
