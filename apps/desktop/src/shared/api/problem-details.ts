import type { ProblemDetails } from "./common";

export class ApiError extends Error {
  public readonly status: number;
  public readonly title: string;
  public readonly detail?: string;
  public readonly code?: string;
  public readonly traceId?: string;
  public readonly errors?: Record<string, string[]>;

  public constructor(status: number, problem?: ProblemDetails) {
    const title = problem?.title ?? `LocalApi request failed: ${status}`;
    const detail = problem?.detail;
    super(detail ? `${title}: ${detail}` : title);
    this.name = "ApiError";
    this.status = problem?.status ?? status;
    this.title = title;
    this.detail = detail;
    this.code = problem?.code;
    this.traceId = problem?.traceId;
    this.errors = problem?.errors;
  }
}

export function getErrorMessage(error: unknown, fallback: string) {
  if (error instanceof ApiError) {
    return error.detail ?? error.title;
  }

  if (error instanceof Error) {
    return error.message;
  }

  return fallback;
}
