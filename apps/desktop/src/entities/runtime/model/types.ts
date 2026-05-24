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
  runningIngestionJobsCount: number;
  cancelledIngestionJobsCount: number;
  lastProcessedIngestionJobId: string | null;
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
