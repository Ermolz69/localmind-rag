import type { WatchedFolderStatusResponse } from "@entities/settings";
import { request } from "./http";

export const watchedFoldersApi = {
  getStatus: () =>
    request<WatchedFolderStatusResponse>("/watched-folders/status"),
};
