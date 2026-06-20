import type { OperationData, OperationJsonBody } from "@shared/contracts";
import { request } from "./http";

export const companionApi = {
  getInfo: () => request<OperationData<"GetCompanionInfo">>("/companion/info"),
  getPairingStatus: () =>
    request<OperationData<"GetCompanionPairingStatus">>("/companion/pairing"),
  startPairing: () =>
    request<OperationData<"StartCompanionPairing">>("/companion/pairing", {
      method: "POST",
    }),
  cancelPairing: () =>
    request<OperationData<"CancelCompanionPairing">>("/companion/pairing", {
      method: "DELETE",
    }),
  confirmPairing: (req: OperationJsonBody<"ConfirmCompanionPairing">) =>
    request<OperationData<"ConfirmCompanionPairing">>(
      "/companion/pairing/confirm",
      {
        method: "POST",
        body: JSON.stringify(req),
      },
    ),
  getDevices: () =>
    request<OperationData<"GetCompanionDevices">>("/companion/devices"),
  revokeDevice: (deviceId: string) =>
    request<OperationData<"RevokeCompanionDevice">>(
      `/companion/devices/${deviceId}`,
      {
        method: "DELETE",
      },
    ),
  getFileRoots: () =>
    request<OperationData<"GetCompanionFileRoots">>("/companion/files/roots"),
  browseFiles: (path: string) =>
    request<OperationData<"BrowseCompanionFiles">>(
      `/companion/files/browse?path=${encodeURIComponent(path)}`,
    ),
  addFile: (req: OperationJsonBody<"AddCompanionFile">) =>
    request<OperationData<"AddCompanionFile">>("/companion/files/add", {
      method: "POST",
      body: JSON.stringify(req),
    }),
};
