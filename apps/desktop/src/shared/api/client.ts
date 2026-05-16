const apiBaseUrl =
  import.meta.env.VITE_LOCAL_API_URL ?? "http://127.0.0.1:49321";

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const isFormData = init?.body instanceof FormData;
  const response = await fetch(`${apiBaseUrl}${path}`, {
    headers: {
      ...(isFormData ? {} : { "Content-Type": "application/json" }),
      ...init?.headers,
    },
    ...init,
  });

  if (!response.ok) {
    throw new Error(`LocalApi request failed: ${response.status}`);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export const localApi = {
  getHealth: () => request<HealthStatus>("/api/health"),
  getRuntimeStatus: () => request<RuntimeStatus>("/api/runtime/status"),
  getSyncStatus: () => request<SyncStatus>("/api/sync/status"),
  getBuckets: () => request<BucketDto[]>("/api/buckets"),
  getDiagnostics: () => request<DiagnosticsStatus>("/api/diagnostics"),
  getSettings: () => request<AppSettings>("/api/settings"),
  createBucket: (name: string) =>
    request<BucketDto>("/api/buckets", {
      method: "POST",
      body: JSON.stringify({ name }),
    }),
  getDocuments: (bucketId?: string) =>
    request<CursorPage<DocumentSummary>>(
      `/api/documents${bucketId ? `?bucketId=${bucketId}` : ""}`,
    ).then((page) => page.items),
  uploadDocument: (file: File, bucketId?: string) => {
    const form = new FormData();
    form.append("file", file);
    return request<UploadDocumentResponse>(
      `/api/documents/upload${bucketId ? `?bucketId=${bucketId}` : ""}`,
      {
        method: "POST",
        body: form,
      },
    );
  },
  saveSettings: (settings: AppSettings) =>
    request<void>("/api/settings", {
      method: "PUT",
      body: JSON.stringify(settings),
    }),
  processIngestionJob: (jobId: string) =>
    request<ProcessIngestionJobResponse>(
      `/api/ingestion/jobs/${jobId}/process`,
      {
        method: "POST",
      },
    ),
  semanticSearch: (query: string) =>
    request<SemanticSearchResponse>("/api/search/semantic", {
      method: "POST",
      body: JSON.stringify({ query }),
    }).then((response) => response.sources),
  getNotes: () =>
    request<CursorPage<NoteDto>>("/api/notes").then((page) => page.items),
  createNote: (note: {
    title: string;
    markdown: string;
    bucketId?: string | null;
  }) =>
    request<NoteDto>("/api/notes", {
      method: "POST",
      body: JSON.stringify(note),
    }),
  updateNote: (
    id: string,
    note: { title: string; markdown: string; bucketId?: string | null },
  ) =>
    request<void>(`/api/notes/${id}`, {
      method: "PUT",
      body: JSON.stringify(note),
    }),
  deleteNote: (id: string) =>
    request<void>(`/api/notes/${id}`, {
      method: "DELETE",
    }),
};

export type HealthStatus = {
  status: string;
  app: string;
};

export type BucketDto = {
  id: string;
  name: string;
  description: string | null;
  syncStatus: string;
  createdAt: string;
  updatedAt: string | null;
};

export type DocumentSummary = {
  id: string;
  name: string;
  status: string;
  createdAt: string;
  lastError: string | null;
};

export type CursorPage<T> = {
  items: T[];
  nextCursor: string | null;
  limit: number;
  hasMore: boolean;
};

export type NoteDto = {
  id: string;
  title: string;
  markdown: string;
  bucketId: string | null;
};

export type UploadDocumentResponse = {
  documentId: string;
  ingestionJobId: string;
  status: string;
};

export type ProcessIngestionJobResponse = {
  jobId: string;
  status: string;
};

export type RuntimeStatus = {
  localApiReady: boolean;
  aiRuntimeStatus: string;
  modelsAvailable: boolean;
  offlineMode: boolean;
};

export type SyncStatus = {
  enabled: boolean;
  online: boolean;
  pendingOperations: number;
  status: string;
};

export type RagSource = {
  documentId: string;
  documentName: string;
  chunkId: string;
  pageNumber: number | null;
  score: number;
  snippet: string;
};

export type SemanticSearchResponse = {
  sources: RagSource[];
};

export type DiagnosticsPaths = {
  databasePath: string;
  filesPath: string;
  indexPath: string;
  logsPath: string;
};

export type DiagnosticsStorage = {
  databaseSizeBytes: number;
  filesSizeBytes: number;
  indexSizeBytes: number;
  logsSizeBytes: number;
};

export type DiagnosticsCounts = {
  bucketsCount: number;
  documentsCount: number;
  documentFilesCount: number;
  documentChunksCount: number;
  documentEmbeddingsCount: number;
  notesCount: number;
  conversationsCount: number;
  pendingIngestionJobsCount: number;
  failedIngestionJobsCount: number;
};

export type DiagnosticsIngestionError = {
  jobId: string;
  documentId: string;
  documentName: string;
  lastError: string;
  processedAt: string | null;
};

export type DiagnosticsRuntime = {
  runtimeMode: string;
  localApiVersion: string;
  aiRuntimeStatus: RuntimeStatus;
};

export type DiagnosticsStatus = {
  paths: DiagnosticsPaths;
  storage: DiagnosticsStorage;
  counts: DiagnosticsCounts;
  latestErrors: DiagnosticsIngestionError[];
  runtime: DiagnosticsRuntime;
};

export type AppSettings = {
  appearance: AppearanceSettings;
  ai: AiSettings;
  runtimePaths: RuntimePathsSettings;
  sync: SyncSettings;
};

export type AppearanceSettings = {
  theme: string;
};

export type AiSettings = {
  provider: string;
  chatModel: string;
  embeddingModel: string;
  runtimePath: string;
  modelsPath: string;
};

export type RuntimePathsSettings = {
  dataPath: string;
  databasePath: string;
  filesPath: string;
  indexPath: string;
  logsPath: string;
};

export type SyncSettings = {
  enabled: boolean;
  autoSync: boolean;
};
