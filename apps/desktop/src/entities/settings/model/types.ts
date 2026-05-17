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
