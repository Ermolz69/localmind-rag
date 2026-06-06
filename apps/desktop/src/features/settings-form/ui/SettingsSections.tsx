import type {
  AppSettings,
  WatchedFolder,
  WatchedFolderStatusResponse,
} from "@entities/settings";
import type { ReactNode } from "react";
import { Input, Select } from "@shared/ui";

type SettingsSectionsProps = {
  draft: AppSettings;
  watchedFolderStatus?: WatchedFolderStatusResponse | null;
  onChange: (settings: AppSettings) => void;
};

export function SettingsSections({
  draft,
  watchedFolderStatus,
  onChange,
}: SettingsSectionsProps) {
  function updateWatchedFolder(index: number, nextFolder: WatchedFolder) {
    const folders = [...draft.watchedFolders.folders];
    folders[index] = nextFolder;

    onChange({
      ...draft,
      watchedFolders: {
        ...draft.watchedFolders,
        folders,
      },
    });
  }

  function removeWatchedFolder(index: number) {
    onChange({
      ...draft,
      watchedFolders: {
        ...draft.watchedFolders,
        folders: draft.watchedFolders.folders.filter(
          (_, itemIndex) => itemIndex !== index,
        ),
      },
    });
  }

  function addWatchedFolder() {
    onChange({
      ...draft,
      watchedFolders: {
        ...draft.watchedFolders,
        folders: [
          ...draft.watchedFolders.folders,
          {
            path: "",
            enabled: true,
            includeSubdirectories: false,
          },
        ],
      },
    });
  }

  return (
    <div className="space-y-6">
      <Section
        id="runtime-paths"
        title="Runtime paths"
        description="Local folders used by the desktop backend."
      >
        <div className="grid gap-3 md:grid-cols-2">
          <TextField
            label="Data path"
            value={draft.runtimePaths.dataPath}
            onChange={(dataPath) =>
              onChange({
                ...draft,
                runtimePaths: { ...draft.runtimePaths, dataPath },
              })
            }
          />
          <TextField
            label="Database path"
            value={draft.runtimePaths.databasePath}
            onChange={(databasePath) =>
              onChange({
                ...draft,
                runtimePaths: { ...draft.runtimePaths, databasePath },
              })
            }
          />
          <TextField
            label="Files path"
            value={draft.runtimePaths.filesPath}
            onChange={(filesPath) =>
              onChange({
                ...draft,
                runtimePaths: { ...draft.runtimePaths, filesPath },
              })
            }
          />
          <TextField
            label="Index path"
            value={draft.runtimePaths.indexPath}
            onChange={(indexPath) =>
              onChange({
                ...draft,
                runtimePaths: { ...draft.runtimePaths, indexPath },
              })
            }
          />
          <TextField
            label="Logs path"
            value={draft.runtimePaths.logsPath}
            onChange={(logsPath) =>
              onChange({
                ...draft,
                runtimePaths: { ...draft.runtimePaths, logsPath },
              })
            }
          />
        </div>
      </Section>

      <Section
        id="ai"
        title="AI"
        description="Runtime provider and model settings used for chat and embeddings."
      >
        <div className="grid gap-3 md:grid-cols-2">
          <TextField
            label="Provider"
            value={draft.ai.provider}
            onChange={(provider) =>
              onChange({ ...draft, ai: { ...draft.ai, provider } })
            }
          />
          <TextField
            label="Chat model"
            value={draft.ai.chatModel}
            onChange={(chatModel) =>
              onChange({ ...draft, ai: { ...draft.ai, chatModel } })
            }
          />
          <TextField
            label="Embedding model"
            value={draft.ai.embeddingModel}
            onChange={(embeddingModel) =>
              onChange({ ...draft, ai: { ...draft.ai, embeddingModel } })
            }
          />
          <TextField
            label="Runtime path"
            value={draft.ai.runtimePath}
            onChange={(runtimePath) =>
              onChange({ ...draft, ai: { ...draft.ai, runtimePath } })
            }
          />
          <TextField
            label="Models path"
            value={draft.ai.modelsPath}
            onChange={(modelsPath) =>
              onChange({ ...draft, ai: { ...draft.ai, modelsPath } })
            }
          />
        </div>
      </Section>

      <Section
        id="sync"
        title="Sync"
        description="Remote synchronization behavior."
      >
        <div className="grid gap-3 md:grid-cols-2">
          <label className="space-y-1 text-sm font-medium text-foreground">
            <span>Sync enabled</span>
            <Select
              value={String(draft.sync.enabled)}
              onChange={(event) =>
                onChange({
                  ...draft,
                  sync: {
                    ...draft.sync,
                    enabled: event.target.value === "true",
                  },
                })
              }
            >
              <option value="false">Disabled</option>
              <option value="true">Enabled</option>
            </Select>
          </label>

          <label className="space-y-1 text-sm font-medium text-foreground">
            <span>Auto sync</span>
            <Select
              value={String(draft.sync.autoSync)}
              onChange={(event) =>
                onChange({
                  ...draft,
                  sync: {
                    ...draft.sync,
                    autoSync: event.target.value === "true",
                  },
                })
              }
            >
              <option value="false">Disabled</option>
              <option value="true">Enabled</option>
            </Select>
          </label>
        </div>
      </Section>

      <Section
        id="watched-folders"
        title="Watched folders"
        description="Automatically create ingestion jobs when files are created, updated, or deleted in selected folders."
      >
        <div className="space-y-4">
          <div className="grid gap-3 md:grid-cols-3">
            <label className="space-y-1 text-sm font-medium text-foreground">
              <span>Auto-ingestion</span>
              <Select
                value={String(draft.watchedFolders.enabled)}
                onChange={(event) =>
                  onChange({
                    ...draft,
                    watchedFolders: {
                      ...draft.watchedFolders,
                      enabled: event.target.value === "true",
                    },
                  })
                }
              >
                <option value="false">Disabled</option>
                <option value="true">Enabled</option>
              </Select>
            </label>

            <label className="space-y-1 text-sm font-medium text-foreground">
              <span>Debounce milliseconds</span>
              <Input
                min={250}
                max={60000}
                type="number"
                value={draft.watchedFolders.debounceMilliseconds}
                onChange={(event) =>
                  onChange({
                    ...draft,
                    watchedFolders: {
                      ...draft.watchedFolders,
                      debounceMilliseconds: Number(event.target.value),
                    },
                  })
                }
              />
            </label>

            <label className="space-y-1 text-sm font-medium text-foreground">
              <span>Delete policy</span>
              <Select
                value={draft.watchedFolders.deletePolicy}
                onChange={(event) =>
                  onChange({
                    ...draft,
                    watchedFolders: {
                      ...draft.watchedFolders,
                      deletePolicy: event.target.value,
                    },
                  })
                }
              >
                <option value="MarkDeleted">Mark deleted</option>
              </Select>
            </label>
          </div>

          <div className="space-y-3">
            {draft.watchedFolders.folders.map((folder, index) => {
              const status = watchedFolderStatus?.folders.find(
                (item) => item.path === folder.path,
              );

              return (
                <div
                  className="rounded-lg border border-border bg-card/50 p-3"
                  key={`watched-folder-${index}`}
                >
                  <div className="grid gap-3 md:grid-cols-[1fr_auto_auto]">
                    <TextField
                      label="Folder path"
                      value={folder.path}
                      onChange={(path) =>
                        updateWatchedFolder(index, { ...folder, path })
                      }
                    />

                    <label className="space-y-1 text-sm font-medium text-foreground">
                      <span>Enabled</span>
                      <Select
                        value={String(folder.enabled)}
                        onChange={(event) =>
                          updateWatchedFolder(index, {
                            ...folder,
                            enabled: event.target.value === "true",
                          })
                        }
                      >
                        <option value="false">Disabled</option>
                        <option value="true">Enabled</option>
                      </Select>
                    </label>

                    <label className="space-y-1 text-sm font-medium text-foreground">
                      <span>Subfolders</span>
                      <Select
                        value={String(folder.includeSubdirectories)}
                        onChange={(event) =>
                          updateWatchedFolder(index, {
                            ...folder,
                            includeSubdirectories:
                              event.target.value === "true",
                          })
                        }
                      >
                        <option value="false">Disabled</option>
                        <option value="true">Enabled</option>
                      </Select>
                    </label>
                  </div>

                  <div className="mt-3 flex flex-wrap items-center gap-2 text-xs text-muted-foreground">
                    <span>Exists: {status?.exists ? "yes" : "unknown"}</span>
                    <span>
                      Watching: {status?.isWatching ? "active" : "inactive"}
                    </span>
                    <span>Pending: {status?.pendingEvents ?? 0}</span>
                    {status?.lastEventAt ? (
                      <span>Last event: {status.lastEventAt}</span>
                    ) : null}
                    {status?.lastError ? (
                      <span className="text-destructive">
                        Error: {status.lastError}
                      </span>
                    ) : null}
                  </div>

                  <button
                    className="text-destructive mt-3 text-sm font-medium hover:underline"
                    type="button"
                    onClick={() => removeWatchedFolder(index)}
                  >
                    Remove folder
                  </button>
                </div>
              );
            })}

            <button
              className="rounded-md border border-border px-3 py-2 text-sm font-medium hover:bg-muted"
              type="button"
              onClick={addWatchedFolder}
            >
              Add watched folder
            </button>
          </div>

          {watchedFolderStatus?.lastError ? (
            <p className="text-destructive text-sm">
              Watcher error: {watchedFolderStatus.lastError}
            </p>
          ) : null}
        </div>
      </Section>
    </div>
  );
}

function Section({
  id,
  title,
  description,
  children,
}: {
  id: string;
  title: string;
  description: string;
  children: ReactNode;
}) {
  return (
    <section className="rounded-xl border border-border bg-card p-5" id={id}>
      <h2 className="text-lg font-semibold text-foreground">{title}</h2>
      <p className="mt-1 text-sm text-muted-foreground">{description}</p>
      <div className="mt-4">{children}</div>
    </section>
  );
}

function TextField({
  label,
  value,
  onChange,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
}) {
  return (
    <label className="space-y-1 text-sm font-medium text-foreground">
      <span>{label}</span>
      <Input value={value} onChange={(event) => onChange(event.target.value)} />
    </label>
  );
}
