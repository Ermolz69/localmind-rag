export type {
  ApiEnvelopeError,
  ApiErrorDetail,
  ApiMetadata,
  ApiResponse,
  CursorPage,
  CursorPageRequest,
} from "@shared/contracts";

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
