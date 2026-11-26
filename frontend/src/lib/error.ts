import axios from "axios";
import { toast } from "@/components/ui/toast";
import { ENV } from "@/config/env";
import { API_ERROR_CODES, SEAT_CONFLICT_ERROR_KEY } from "@/config/api-error-codes";
import {
  ERROR_MESSAGES,
  ERROR_TOAST_DEDUPE_MS,
  LOAD_ERROR_COPY,
  type LoadErrorCopy,
  SERVER_ERROR_RULE,
  SILENT_TOAST_STATUSES,
  STATUS_ERROR_RULES,
} from "@/config/error-messages";
import { HTTP_STATUS } from "@/config/http-status";

export interface ApiErrorShape {
  message: string;
  errors?: Record<string, string[]>;
  statusCode?: number;
  errorCode?: string;
}

export function extractApiError(error: unknown): ApiErrorShape {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as Partial<ApiErrorShape> | undefined;
    return {
      message: data?.message ?? error.message ?? "Đã có lỗi xảy ra",
      errors: data?.errors ?? undefined,
      statusCode: error.response?.status,
      errorCode: typeof data?.errorCode === "string" ? data.errorCode : undefined,
    };
  }
  if (error instanceof Error) {
    return { message: error.message };
  }
  return { message: ERROR_MESSAGES.unknown };
}

export function describeLoadError(error: unknown): LoadErrorCopy {
  if (axios.isAxiosError(error)) {
    const status = error.response?.status;
    if (status === undefined) {
      return error.code === "ECONNABORTED" || error.code === "ETIMEDOUT"
        ? LOAD_ERROR_COPY.timeout
        : LOAD_ERROR_COPY.network;
    }
    if (status === HTTP_STATUS.forbidden) return LOAD_ERROR_COPY.forbidden;
    if (status === HTTP_STATUS.tooManyRequests) return LOAD_ERROR_COPY.rateLimited;
    if (status >= HTTP_STATUS.serverError) return LOAD_ERROR_COPY.server;
  }
  return LOAD_ERROR_COPY.fallback;
}

export function isNotFoundError(error: unknown): boolean {
  return extractApiError(error).statusCode === HTTP_STATUS.notFound;
}

export function isPasswordChangeRequiredError(error: unknown): boolean {
  const { statusCode, errorCode } = extractApiError(error);
  return statusCode === HTTP_STATUS.forbidden && errorCode === API_ERROR_CODES.passwordChangeRequired;
}

export interface SeatPriceChange {
  showtimeSeatId: number;
  ticketTypeId?: number | null;
  previousPrice?: number | null;
  newPrice: number;
}

export interface PriceChangeRequired {
  seats: SeatPriceChange[];
  newTicketTotal: number;
}

export function getPriceChangeRequired(error: unknown): PriceChangeRequired | null {
  if (!axios.isAxiosError(error)) return null;
  const data = error.response?.data as
    | { errorCode?: unknown; data?: Partial<PriceChangeRequired> | null }
    | undefined;
  if (
    error.response?.status !== HTTP_STATUS.conflict ||
    data?.errorCode !== API_ERROR_CODES.priceChangeConfirmationRequired ||
    !Array.isArray(data.data?.seats)
  ) {
    return null;
  }
  return {
    seats: data.data.seats,
    newTicketTotal: Number(data.data.newTicketTotal ?? 0),
  };
}

export function isComboOutOfStockError(error: unknown): boolean {
  if (!axios.isAxiosError(error)) return false;
  if (error.response?.status !== HTTP_STATUS.conflict) return false;
  const data = error.response.data as { errorCode?: unknown } | undefined;
  return data?.errorCode === API_ERROR_CODES.comboOutOfStock;
}

export function isGenericConflictError(error: unknown): boolean {
  if (!axios.isAxiosError(error)) return false;
  if (error.response?.status !== HTTP_STATUS.conflict) return false;
  const data = error.response.data as
    | { errorCode?: unknown; errors?: Record<string, unknown> | null }
    | undefined;
  if (data?.errorCode) return false;
  if (getPriceChangeRequired(error)) return false;
  const errorKeys = Object.keys(data?.errors ?? {});
  return !errorKeys.some(
    (key) => key.toLowerCase() === SEAT_CONFLICT_ERROR_KEY.toLowerCase(),
  );
}

export function logDevError(error: unknown, context?: string): void {
  if (!ENV.IS_DEV) return;

  const tag = context ? `[${context}]` : "[API Error]";

  if (axios.isAxiosError(error)) {
    console.group(
      `${tag} Axios error — ${error.response?.status ?? "network"}`,
    );
    console.error("URL:", error.config?.url);
    console.error("Method:", error.config?.method?.toUpperCase());
    console.error(
      "Status:",
      error.response?.status,
      error.response?.statusText,
    );
    console.error("Response data:", error.response?.data);
    console.error(
      "Request data:",
      (() => {
        try {
          return JSON.parse(error.config?.data ?? "null");
        } catch {
          return error.config?.data;
        }
      })(),
    );
    console.error("Full error:", error);
    console.groupEnd();
  } else {
    console.error(`${tag}`, error);
  }
}

export function isCancelledError(error: unknown): boolean {
  return axios.isCancel(error);
}

export function isNetworkError(error: unknown): boolean {
  return axios.isAxiosError(error) && !error.response && !axios.isCancel(error);
}

function getServerMessage(error: unknown): string | undefined {
  if (!axios.isAxiosError(error)) return undefined;
  const message = (error.response?.data as Partial<ApiErrorShape> | undefined)
    ?.message;
  return typeof message === "string" && message !== "" ? message : undefined;
}

export function getDisplayMessage(
  error: unknown,
  options?: { preferServerMessage?: boolean },
): string {
  const { message, statusCode } = extractApiError(error);

  if (isNetworkError(error)) {
    const code = (error as { code?: string }).code;
    if (code === "ECONNABORTED" || code === "ETIMEDOUT") {
      return ERROR_MESSAGES.timeout;
    }
    return navigator.onLine ? ERROR_MESSAGES.network : ERROR_MESSAGES.offline;
  }

  if (statusCode) {
    if (options?.preferServerMessage && statusCode < HTTP_STATUS.serverError) {
      const serverMessage = getServerMessage(error);
      if (serverMessage) return serverMessage;
    }

    const rule =
      statusCode >= HTTP_STATUS.serverError ? SERVER_ERROR_RULE : STATUS_ERROR_RULES[statusCode];
    if (rule) {
      return rule.preferServerMessage ? message || rule.message : rule.message;
    }
  }

  if (!navigator.onLine) return ERROR_MESSAGES.offline;

  return message || ERROR_MESSAGES.fallback;
}

let lastToast = { key: "", at: 0 };

export function showApiErrorToast(
  error: unknown,
  opts?: { title?: string },
): void {
  if (isCancelledError(error)) return;
  if (isPasswordChangeRequiredError(error)) return;
  if (getPriceChangeRequired(error)) return;

  const { statusCode } = extractApiError(error);
  if (statusCode && SILENT_TOAST_STATUSES.includes(statusCode)) return;

  const title = opts?.title ?? ERROR_MESSAGES.toastTitle;
  const description = getDisplayMessage(error);

  const key = `${title}|${description}`;
  const now = Date.now();
  if (key === lastToast.key && now - lastToast.at < ERROR_TOAST_DEDUPE_MS)
    return;
  lastToast = { key, at: now };

  toast.add({ title, description, type: "error" });
}

export interface HandleApiErrorOptions {
  context?: string;
  title?: string;
  silent?: boolean;
}

export function handleApiError(
  error: unknown,
  opts: HandleApiErrorOptions = {},
): void {
  logDevError(error, opts.context);
  if (opts.silent || isCustomerGateError(error)) return;
  showApiErrorToast(error, { title: opts.title });
}

function toFieldPath(key: string): string {
  return key.replace(
    /(^|\.)([A-Z])/g,
    (_match, separator: string, char: string) => separator + char.toLowerCase(),
  );
}

export interface CustomerRestrictionNotice {
  reason: string;
  restrictedUntil: string;
  message: string;
}

export function getCustomerRestrictionNotice(error: unknown): CustomerRestrictionNotice | null {
  if (!axios.isAxiosError(error)) return null;
  const body = error.response?.data as
    | {
        message?: unknown;
        errorCode?: unknown;
        data?: { restrictionReason?: unknown; restrictedUntil?: unknown } | null;
      }
    | undefined;
  if (body?.errorCode !== API_ERROR_CODES.pendingOrdersLimitExceeded) return null;
  const reason = body.data?.restrictionReason;
  const restrictedUntil = body.data?.restrictedUntil;
  if (typeof reason !== "string" || typeof restrictedUntil !== "string") return null;
  return {
    reason,
    restrictedUntil,
    message: typeof body.message === "string" ? body.message : "",
  };
}

export function isVerificationRequiredError(error: unknown): boolean {
  const { statusCode, errorCode } = extractApiError(error);
  return statusCode === HTTP_STATUS.forbidden && errorCode === API_ERROR_CODES.verificationRequired;
}

export function isCustomerGateError(error: unknown): boolean {
  return getCustomerRestrictionNotice(error) !== null || isVerificationRequiredError(error);
}

export function getGiftCardLockSeconds(error: unknown): number | null {
  if (!axios.isAxiosError(error)) return null;
  const body = error.response?.data as
    | { errorCode?: string; data?: { retryAfterSeconds?: number } | null }
    | undefined;
  if (body?.errorCode !== API_ERROR_CODES.giftCardAttemptsLocked) return null;
  const seconds = body.data?.retryAfterSeconds;
  return typeof seconds === "number" && seconds > 0 ? seconds : 0;
}

export function getFieldErrors(error: unknown): Record<string, string> {
  const { errors } = extractApiError(error);
  if (!errors) return {};
  return Object.fromEntries(
    Object.entries(errors).map(([key, msgs]) => [
      toFieldPath(key),
      msgs[0] ?? "",
    ]),
  );
}

export function hasFieldErrors(error: unknown): boolean {
  return Object.keys(getFieldErrors(error)).length > 0;
}
