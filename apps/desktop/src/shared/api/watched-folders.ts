import type {
  WatchedFolderCleanupResponse,
  WatchedFolderStatusResponse,
} from "@entities/settings";
import { request } from "./http";

export const watchedFoldersApi = {
  getStatus: () =>
    request<WatchedFolderStatusResponse>("/watched-folders/status"),
  cleanup: () =>
    request<WatchedFolderCleanupResponse>("/watched-folders/cleanup", {
      method: "POST",
    }),
};
