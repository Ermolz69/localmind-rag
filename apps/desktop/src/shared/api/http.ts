import type { ApiResponse } from "./common";
import { ApiError } from "./problem-details";

const apiBaseUrl =
  import.meta.env.VITE_LOCAL_API_URL ?? "http://127.0.0.1:49321";

const publicApiPrefix = "/api/v1";

function isJsonResponse(response: Response) {
  return response.headers.get("content-type")?.includes("application/json");
}

function isApiResponse(body: unknown): body is ApiResponse<unknown> {
  return (
    typeof body === "object" &&
    body !== null &&
    "success" in body &&
    "metadata" in body
  );
}

async function readJson(response: Response) {
  if (!isJsonResponse(response)) {
    return undefined;
  }

  try {
    return (await response.json()) as unknown;
  } catch {
    return undefined;
  }
}

export async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const isFormData = init?.body instanceof FormData;

  const response = await fetch(`${apiBaseUrl}${publicApiPrefix}${path}`, {
    headers: {
      ...(isFormData ? {} : { "Content-Type": "application/json" }),
      ...init?.headers,
    },
    ...init,
  });

  if (response.status === 204) {
    return undefined as T;
  }

  const body = await readJson(response);

  if (body === undefined) {
    if (!response.ok) {
      throw new ApiError(response.status);
    }

    return undefined as T;
  }

  if (!isApiResponse(body)) {
    if (!response.ok) {
      throw new ApiError(response.status);
    }

    return body as T;
  }

  if (!response.ok || !body.success) {
    throw new ApiError(response.status, body.error, body.metadata.requestId);
  }

  return body.data as T;
}
