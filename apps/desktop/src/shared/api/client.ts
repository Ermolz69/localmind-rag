const apiBaseUrl =
  import.meta.env.VITE_LOCAL_API_URL ?? "http://127.0.0.1:49321";

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    headers: {
      "Content-Type": "application/json",
      ...init?.headers,
    },
    ...init,
  });

  if (!response.ok) {
    throw new Error(`LocalApi request failed: ${response.status}`);
  }

  return (await response.json()) as T;
}

export const localApi = {
  health: () => request<{ status: string }>("/api/health"),
  runtimeStatus: () => request<RuntimeStatus>("/api/runtime/status"),
  documents: () => request<DocumentSummary[]>("/api/documents"),
  semanticSearch: (query: string) =>
    request<RagSource[]>("/api/search/semantic", {
      method: "POST",
      body: JSON.stringify({ query }),
    }),
};

export type DocumentSummary = {
  id: string;
  name: string;
  status: string;
  createdAt: string;
};

export type RuntimeStatus = {
  localApiReady: boolean;
  aiRuntimeStatus: string;
  modelsAvailable: boolean;
  offlineMode: boolean;
};

export type RagSource = {
  documentId: string;
  documentName: string;
  chunkId: string;
  pageNumber: number | null;
  score: number;
  snippet: string;
};
