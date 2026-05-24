export type CursorPage<T> = {
  items: T[];
  nextCursor: string | null;
  limit: number;
  hasMore: boolean;
};

export type CursorPageRequest = {
  cursor?: string | null;
  limit?: number;
};

export type ProblemDetails = {
  title?: string;
  status?: number;
  detail?: string;
  traceId?: string;
  code?: string;
  errors?: Record<string, string[]>;
};

export type ApiErrorDetail = {
  field?: string | null;
  message: string;
};

export type ApiEnvelopeError = {
  code: string;
  message: string;
  details?: ApiErrorDetail[] | null;
};

export type ApiMetadata = {
  timestamp: string;
  requestId?: string | null;
};

export type ApiResponse<T> = {
  success: boolean;
  data: T | null;
  error: ApiEnvelopeError | null;
  metadata: ApiMetadata;
};

export function toQueryString(
  params: Record<string, string | number | null | undefined>,
) {
  const search = new URLSearchParams();

  Object.entries(params).forEach(([key, value]) => {
    if (value !== null && value !== undefined && value !== "") {
      search.set(key, String(value));
    }
  });

  const query = search.toString();
  return query ? `?${query}` : "";
}
