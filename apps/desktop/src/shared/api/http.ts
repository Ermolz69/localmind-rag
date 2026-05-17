import type { ProblemDetails } from "./common";
import { ApiError } from "./problem-details";

const apiBaseUrl =
  import.meta.env.VITE_LOCAL_API_URL ?? "http://127.0.0.1:49321";

function isJsonResponse(response: Response) {
  return response.headers.get("content-type")?.includes("application/json");
}

async function readProblemDetails(response: Response) {
  if (!isJsonResponse(response)) {
    return undefined;
  }

  try {
    return (await response.json()) as ProblemDetails;
  } catch {
    return undefined;
  }
}

export async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const isFormData = init?.body instanceof FormData;
  const response = await fetch(`${apiBaseUrl}${path}`, {
    headers: {
      ...(isFormData ? {} : { "Content-Type": "application/json" }),
      ...init?.headers,
    },
    ...init,
  });

  if (!response.ok) {
    throw new ApiError(response.status, await readProblemDetails(response));
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}
