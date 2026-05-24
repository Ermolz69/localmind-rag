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
