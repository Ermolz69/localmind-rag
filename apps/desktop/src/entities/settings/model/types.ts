export type AppSettings = {
  appearance: AppearanceSettings;
  ai: AiSettings;
  runtimePaths: RuntimePathsSettings;
  sync: SyncSettings;
  watchedFolders: WatchedFoldersSettings;
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

export type WatchedFoldersSettings = {
  enabled: boolean;
  debounceMilliseconds: number;
  deletePolicy: string;
  folders: WatchedFolder[];
};

export type WatchedFolder = {
  path: string;
  enabled: boolean;
  includeSubdirectories: boolean;
};

export type WatchedFolderCleanupResponse = {
  cleanedCount: number;
};

export type WatchedFolderStatusResponse = {
  enabled: boolean;
  debounceMilliseconds: number;
  pendingEvents: number;
  deletePolicy: string;
  lastError: string | null;
  lastErrorAt: string | null;
  folders: WatchedFolderStatus[];
};

export type WatchedFolderStatus = {
  path: string;
  enabled: boolean;
  includeSubdirectories: boolean;
  exists: boolean;
  isWatching: boolean;
  pendingEvents: number;
  lastEventAt: string | null;
  lastError: string | null;
  lastErrorAt: string | null;
  healthStatus: string;
  lastScanStartedAt: string | null;
  lastScanCompletedAt: string | null;
  activeDocumentsCount: number;
  deletedWaitingCleanupCount: number;
  lastScanNewFiles: number;
  lastScanChangedFiles: number;
  lastScanDeletedFiles: number;
  lastScanUnchangedFiles: number;
  lastScanUnsupportedFiles: number;
};

export type RescanWatchedFoldersRequest = {
  path?: string | null;
};

export type RescanWatchedFoldersResponse = {
  scannedFolders: number;
  queuedCreatedOrChanged: number;
  queuedDeleted: number;
  unchangedFiles: number;
  unsupportedFiles: number;
  failedFolders: number;
};
