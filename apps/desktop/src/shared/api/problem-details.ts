import type { ApiEnvelopeError, ApiErrorDetail } from "./common";

export class ApiError extends Error {
  public readonly status: number;
  public readonly code?: string;
  public readonly details?: ApiErrorDetail[] | null;
  public readonly requestId?: string | null;

  public constructor(
    status: number,
    error?: ApiEnvelopeError | null,
    requestId?: string | null,
  ) {
    const message = error?.message ?? `LocalApi request failed: ${status}`;
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.code = error?.code;
    this.details = error?.details;
    this.requestId = requestId;
  }
}

export function getErrorMessage(error: unknown, fallback: string) {
  if (error instanceof ApiError) {
    return error.message;
  }

  if (error instanceof Error) {
    return error.message;
  }

  return fallback;
}

export function getFieldErrors(error: unknown): Record<string, string[]> {
  if (!(error instanceof ApiError) || !error.details) {
    return {};
  }

  return error.details.reduce<Record<string, string[]>>((fields, detail) => {
    if (!detail.field) {
      return fields;
    }

    fields[detail.field] = [...(fields[detail.field] ?? []), detail.message];
    return fields;
  }, {});
}

export function getFieldError(error: unknown, field: string) {
  return getFieldErrors(error)[field]?.[0] ?? null;
}
