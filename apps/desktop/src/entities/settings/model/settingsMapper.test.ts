import { describe, expect, it } from "vitest";

import type { OperationData } from "@shared/contracts";

import { toAppSettings } from "./settingsMapper";

describe("toAppSettings", () => {
  it("fills watched folder defaults when the backend omits them", () => {
    const settings: OperationData<"GetSettings"> = {
      appearance: { theme: "System" },
      ai: {
        provider: "llama-cpp",
        chatModel: "chat.gguf",
        embeddingModel: "embedding.gguf",
        runtimePath: "runtime",
        modelsPath: "models",
      },
      runtimePaths: {
        dataPath: "data",
        databasePath: "data/localmind.db",
        filesPath: "files",
        indexPath: "indexes",
        logsPath: "logs",
      },
      sync: { enabled: false, autoSync: false },
    };

    const mappedSettings = toAppSettings(settings);

    expect(mappedSettings.diagnostics).toEqual({
      enabled: true,
      developerModeEnabled: false,
      minimumLogLevel: "Information",
      useSeparateLogFiles: false,
      enableErrorLogs: true,
      enableSqlLogs: false,
      enableHttpLogs: true,
      enableDiagnosticEventLogs: false,
      enableDebugTrace: false,
      logRetainedDays: 14,
    });
    expect(mappedSettings.watchedFolders).toEqual({
      enabled: false,
      debounceMilliseconds: 1000,
      deletePolicy: "MarkDeleted",
      folders: [],
      ignoredFolders: [".git", "node_modules", "bin", "obj"],
      ignoredPatterns: ["~$*", "*.tmp", "*.bak"],
      maxFileSizeMb: 100,
      allowedExtensions: null,
      storageMode: "LinkOnly",
    });
    expect(mappedSettings.companionMode).toEqual({ enabled: false });
  });

  it("keeps companion mode when the backend provides it", () => {
    const settings: OperationData<"GetSettings"> = {
      appearance: { theme: "System" },
      ai: {
        provider: "llama-cpp",
        chatModel: "chat.gguf",
        embeddingModel: "embedding.gguf",
        runtimePath: "runtime",
        modelsPath: "models",
      },
      runtimePaths: {
        dataPath: "data",
        databasePath: "data/localmind.db",
        filesPath: "files",
        indexPath: "indexes",
        logsPath: "logs",
      },
      sync: { enabled: false, autoSync: false },
      companionMode: { enabled: true },
    };

    const mappedSettings = toAppSettings(settings);

    expect(mappedSettings.companionMode).toEqual({ enabled: true });
  });
});
