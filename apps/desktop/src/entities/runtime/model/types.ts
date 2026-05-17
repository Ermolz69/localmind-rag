export type HealthStatus = {
  status: string;
  app: string;
};

export type RuntimeStatus = {
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
  aiRuntimeStatus: RuntimeStatus;
};

export type DiagnosticsStatus = {
  paths: DiagnosticsPaths;
  storage: DiagnosticsStorage;
  counts: DiagnosticsCounts;
  latestErrors: DiagnosticsIngestionError[];
  runtime: DiagnosticsRuntime;
};
