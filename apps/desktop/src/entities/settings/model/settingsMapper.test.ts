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

    expect(toAppSettings(settings).watchedFolders).toEqual({
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
  });
});
