export type HealthStatus = {
  status: string;
  app: string;
};

export type RuntimeStatus = {
  localApiReady: boolean;
  aiRuntimeStatus: string;
  modelsAvailable: boolean;
  offlineMode: boolean;
  runtimePath: string | null;
  modelPath: string | null;
  setupRequired: boolean;
  setupReason: string | null;
  chatModelName: string | null;
  embeddingModelName: string | null;
  chatModelPath: string | null;
  embeddingModelPath: string | null;
};

export type RuntimeSetupResponse = {
  runtimeInstalled: boolean;
  modelInstalled: boolean;
  message: string;
  status: RuntimeStatus;
};

export type SyncStatus = {
  enabled: boolean;
  online: boolean;
  pendingOperations: number;
  status: string;
};

export type DiagnosticsHealthStatus = "Healthy" | "Degraded" | "Unhealthy";

export type DiagnosticsDatabase = {
  status: DiagnosticsHealthStatus;
  bucketsCount: number;
  documentsCount: number;
  documentFilesCount: number;
  notesCount: number;
  conversationsCount: number;
  pendingIngestionJobsCount: number;
  runningIngestionJobsCount: number;
  failedIngestionJobsCount: number;
  cancelledIngestionJobsCount: number;
  lastProcessedIngestionJobId: string | null;
};

export type DiagnosticsVectorIndex = {
  status: DiagnosticsHealthStatus;
  documentChunksCount: number;
  documentEmbeddingsCount: number;
};

export type DiagnosticsStorage = {
  status: DiagnosticsHealthStatus;
  databaseSizeBytes: number;
  filesSizeBytes: number;
  indexSizeBytes: number;
  logsSizeBytes: number;
};

export type DiagnosticsRuntime = {
  status: DiagnosticsHealthStatus;
  runtimeMode: string;
  localApiVersion: string;
  aiRuntimeStatus: RuntimeStatus;
};

export type DiagnosticsIngestionError = {
  jobId: string;
  documentId: string;
  documentName: string;
  errorCode: string;
  errorMessage: string;
  processedAt: string | null;
  retryCount: number;
  lastOperationId: string | null;
};

export type DiagnosticsStatus = {
  status: DiagnosticsHealthStatus;
  database: DiagnosticsDatabase;
  storage: DiagnosticsStorage;
  vectorIndex: DiagnosticsVectorIndex;
  runtime: DiagnosticsRuntime;
  latestErrors: DiagnosticsIngestionError[];
};
