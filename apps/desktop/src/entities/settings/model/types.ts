import type {
  OperationData,
  OperationJsonBody,
  Schema,
} from "@shared/contracts";

type AppSettingsDto = OperationData<"GetSettings">;

export type AppearanceSettings = Schema<"AppearanceSettingsDto">;
export type AiSettings = Schema<"AiSettingsDto">;
export type RuntimePathsSettings = Schema<"RuntimePathsSettingsDto">;
export type SyncSettings = Schema<"SyncSettingsDto">;
export type WatchedFoldersSettings = Schema<"WatchedFoldersSettingsDto">;
export type WatchedFolder = Schema<"WatchedFolderDto">;
export type WatchedFolderCleanupResponse =
  OperationData<"CleanupWatchedFolders">;
export type WatchedFolderStatusResponse =
  OperationData<"GetWatchedFolderStatus">;
export type WatchedFolderStatus = Schema<"WatchedFolderStatusDto">;
export type RescanWatchedFoldersRequest =
  OperationJsonBody<"RescanWatchedFolders">;
export type RescanWatchedFoldersResponse =
  OperationData<"RescanWatchedFolders">;

export type AppSettings = Omit<AppSettingsDto, "watchedFolders"> & {
  watchedFolders: WatchedFoldersSettings;
};
