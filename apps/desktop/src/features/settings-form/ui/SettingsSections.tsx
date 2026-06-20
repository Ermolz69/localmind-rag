import type {
  AppSettings,
  WatchedFolder,
  WatchedFolderStatusResponse,
} from "@entities/settings";
import { useState, type ReactNode } from "react";
import { ExternalLink, FolderOpen } from "lucide-react";
import {
  Input,
  Select,
  Switch,
  Badge,
  Button,
  ConfirmDialog,
  Toast,
} from "@shared/ui";
import { diagnosticsApi, watchedFoldersApi } from "@shared/api";
import { useToast } from "@shared/lib/hooks";
import { cn } from "@shared/lib/cn";
import {
  openPathInExplorer,
  pickFolder,
  revealFileInExplorer,
} from "@shared/lib/desktop";

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
  const [isCleanupConfirmOpen, setCleanupConfirmOpen] = useState(false);
  const [isCleaning, setIsCleaning] = useState(false);
  const [isRescanningAll, setIsRescanningAll] = useState(false);
  const [rescanningPath, setRescanningPath] = useState<string | null>(null);
  const [isClearLogsConfirmOpen, setClearLogsConfirmOpen] = useState(false);
  const [isClearingLogs, setIsClearingLogs] = useState(false);
  const { toast, showToast, dismissToast } = useToast();
  const developerModeEnabled = draft.diagnostics?.developerModeEnabled ?? false;
  const useSeparateLogFiles = draft.diagnostics?.useSeparateLogFiles ?? false;
  const companionModeEnabled = draft.companionMode?.enabled ?? false;
  const companionStatusLabel = companionModeEnabled
    ? "Waiting for connection"
    : "Off";

  function setDiagnostics(patch: Partial<AppSettings["diagnostics"]>) {
    onChange({
      ...draft,
      diagnostics: { ...draft.diagnostics, ...patch },
    });
  }

  async function handleClearLogs() {
    setIsClearingLogs(true);
    try {
      const result = await diagnosticsApi.cleanupLogs();
      const freedKb = Math.round(result.freedBytes / 1024);
      showToast(
        `Removed ${result.deletedFiles} log file(s), freed ${freedKb} KB. Skipped ${result.skippedFiles} in-use file(s).`,
        "success",
      );
    } catch (error) {
      console.error(error);
      showToast("Failed to clear logs.", "error");
    } finally {
      setIsClearingLogs(false);
      setClearLogsConfirmOpen(false);
    }
  }

  async function handleRescan(path?: string) {
    if (path) {
      setRescanningPath(path);
    } else {
      setIsRescanningAll(true);
    }

    try {
      const response = await watchedFoldersApi.rescan({ path: path || null });
      const checked =
        response.queuedCreatedOrChanged +
        response.unchangedFiles +
        response.unsupportedFiles;
      showToast(
        `Rescan completed: ${checked} files checked, ${response.queuedDeleted} missing files detected.`,
        "success",
      );
    } catch (error) {
      console.error(error);
      showToast("Failed to rescan watched folders.", "error");
    } finally {
      if (path) {
        setRescanningPath(null);
      } else {
        setIsRescanningAll(false);
      }
    }
  }

  async function handleCleanup() {
    setIsCleaning(true);
    try {
      const response = await watchedFoldersApi.cleanup();
      showToast(
        `Cleaned ${response.cleanedCount} deleted watched documents from LocalMind.`,
        "success",
      );
    } catch (error) {
      console.error(error);
      showToast("Failed to clean up watched documents.", "error");
    } finally {
      setIsCleaning(false);
      setCleanupConfirmOpen(false);
    }
  }

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
      {developerModeEnabled ? (
        <Section
          id="runtime-paths"
          title="Runtime paths"
          description="Local folders used by the desktop backend. Changing these paths may require restarting LocalMind."
        >
          <div className="grid gap-3">
            <PathField
              kind="folder"
              label="Data path"
              description="Root folder for all local app data."
              value={draft.runtimePaths.dataPath}
              onChange={(dataPath) =>
                onChange({
                  ...draft,
                  runtimePaths: { ...draft.runtimePaths, dataPath },
                })
              }
            />
            <PathField
              kind="file"
              label="Database path"
              description="Location of the SQLite database file."
              value={draft.runtimePaths.databasePath}
              onChange={(databasePath) =>
                onChange({
                  ...draft,
                  runtimePaths: { ...draft.runtimePaths, databasePath },
                })
              }
            />
            <PathField
              kind="folder"
              label="Files path"
              description="Where imported document files are stored."
              value={draft.runtimePaths.filesPath}
              onChange={(filesPath) =>
                onChange({
                  ...draft,
                  runtimePaths: { ...draft.runtimePaths, filesPath },
                })
              }
            />
            <PathField
              kind="folder"
              label="Index path"
              description="Where the vector search index is stored."
              value={draft.runtimePaths.indexPath}
              onChange={(indexPath) =>
                onChange({
                  ...draft,
                  runtimePaths: { ...draft.runtimePaths, indexPath },
                })
              }
            />
            <PathField
              kind="folder"
              label="Logs path"
              description="Where LocalMind writes its log files."
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
      ) : null}

      <Section
        id="ai"
        title="AI"
        description="Runtime provider and model settings used for chat and embeddings."
      >
        <div className="grid gap-3 md:grid-cols-2">
          <Field
            label="Provider"
            description="Local runtime that loads and runs the models."
          >
            <Select
              value={draft.ai.provider}
              onChange={(event) =>
                onChange({
                  ...draft,
                  ai: { ...draft.ai, provider: event.target.value },
                })
              }
            >
              <option value="LlamaCpp">LlamaCpp</option>
              <option value="Ollama">Ollama</option>
            </Select>
          </Field>
          <TextField
            label="Chat model"
            description="Generates answers in chat."
            value={draft.ai.chatModel}
            onChange={(chatModel) =>
              onChange({ ...draft, ai: { ...draft.ai, chatModel } })
            }
          />
          <TextField
            label="Embedding model"
            description="Encodes documents into vectors for semantic search."
            value={draft.ai.embeddingModel}
            onChange={(embeddingModel) =>
              onChange({ ...draft, ai: { ...draft.ai, embeddingModel } })
            }
          />
          {developerModeEnabled ? (
            <>
              <PathField
                kind="file"
                label="Runtime path"
                description="Path to the runtime server executable."
                value={draft.ai.runtimePath}
                onChange={(runtimePath) =>
                  onChange({ ...draft, ai: { ...draft.ai, runtimePath } })
                }
              />
              <PathField
                kind="folder"
                label="Models path"
                description="Folder where downloaded model files are stored."
                value={draft.ai.modelsPath}
                onChange={(modelsPath) =>
                  onChange({ ...draft, ai: { ...draft.ai, modelsPath } })
                }
              />
            </>
          ) : null}
        </div>
      </Section>

      <Section
        id="sync"
        title="Sync"
        description="Remote synchronization behavior — not available yet."
        badge={
          <Badge className="border-accent/40 bg-accent/10 text-accent">
            In development
          </Badge>
        }
      >
        <div className="grid gap-3 md:grid-cols-2">
          <Field
            label="Sync enabled"
            description="Turn remote synchronization on or off."
          >
            <Select
              disabled
              title="Sync is not available yet."
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
          </Field>

          <Field
            label="Auto sync"
            description="Push and pull changes automatically in the background."
          >
            <Select
              disabled
              title="Sync is not available yet."
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
          </Field>
        </div>
      </Section>

      <Section
        id="companion-mode"
        title="Companion Mode"
        description="Let a phone connect to LocalMind over your local network as a remote interface. The desktop app stays local-only until you explicitly turn this on."
        badge={
          <Badge className="border-accent/40 bg-accent/10 text-accent">
            Local network
          </Badge>
        }
      >
        <div className="divide-y divide-border rounded-lg border border-border">
          <SettingRow
            title="Enable phone connection"
            description="Off by default. You decide when to allow a phone to connect."
            control={
              <Switch
                checked={companionModeEnabled}
                onChange={(enabled) =>
                  onChange({
                    ...draft,
                    companionMode: { ...draft.companionMode, enabled },
                  })
                }
                aria-label="Enable phone connection"
              />
            }
          />
          <SettingRow
            title="Status"
            description="Current state of the phone connection."
            control={
              <span
                className={cn(
                  "rounded-full px-2 py-0.5 text-xs font-medium",
                  companionModeEnabled
                    ? "bg-accent/10 text-accent"
                    : "bg-muted text-muted-foreground",
                )}
              >
                {companionStatusLabel}
              </span>
            }
          />
          <SettingRow
            title="Network"
            description="Companion Mode only accepts connections on your local Wi-Fi."
            control={
              <span className="text-sm text-muted-foreground">Local Wi-Fi</span>
            }
          />
          <SettingRow
            title="Device"
            description="The phone currently paired with this computer."
            control={
              <span className="text-sm text-muted-foreground">
                No device connected
              </span>
            }
          />
        </div>

        <p className="mt-4 text-xs leading-relaxed text-muted-foreground">
          Pairing a phone and browsing from it arrive in a later step. For now
          this is the safe, opt-in switch that turns the mode on or off.
        </p>
      </Section>

      <Section
        id="diagnostics"
        title="Diagnostics"
        description="Control the diagnostics panel and how LocalMind writes its log files."
      >
        <div className="divide-y divide-border rounded-lg border border-border">
          <SettingRow
            title="Diagnostics enabled"
            description="Show the diagnostics panel and page inside the app."
            control={
              <Switch
                checked={draft.diagnostics?.enabled ?? true}
                onChange={(enabled) => setDiagnostics({ enabled })}
                aria-label="Diagnostics enabled"
              />
            }
          />
          <SettingRow
            title="Developer mode"
            description="Reveal advanced logging controls and runtime paths."
            control={
              <Switch
                checked={developerModeEnabled}
                onChange={(developerModeEnabled) =>
                  setDiagnostics({ developerModeEnabled })
                }
                aria-label="Developer mode"
              />
            }
          />
        </div>

        {developerModeEnabled ? (
          <div className="mt-5 space-y-5">
            <SettingGroup title="Logging">
              <SettingRow
                title="Minimum log level"
                description="Lowest severity written to the logs."
                control={
                  <Select
                    className="w-44"
                    value={draft.diagnostics.minimumLogLevel}
                    onChange={(event) =>
                      setDiagnostics({ minimumLogLevel: event.target.value })
                    }
                  >
                    <option value="Trace">Trace</option>
                    <option value="Debug">Debug</option>
                    <option value="Information">Information</option>
                    <option value="Warning">Warning</option>
                    <option value="Error">Error</option>
                    <option value="Critical">Critical</option>
                    <option value="None">None</option>
                  </Select>
                }
              />
              <SettingRow
                title="HTTP logs"
                description="Log every request. When off, only warnings and errors."
                control={
                  <Switch
                    checked={draft.diagnostics.enableHttpLogs}
                    onChange={(enableHttpLogs) =>
                      setDiagnostics({ enableHttpLogs })
                    }
                    aria-label="HTTP logs"
                  />
                }
              />
              <SettingRow
                title="SQL logs"
                description="Log every query. When off, only warnings and errors."
                control={
                  <Switch
                    checked={draft.diagnostics.enableSqlLogs}
                    onChange={(enableSqlLogs) =>
                      setDiagnostics({ enableSqlLogs })
                    }
                    aria-label="SQL logs"
                  />
                }
              />
            </SettingGroup>

            <SettingGroup title="Log files">
              <SettingRow
                title="Separate log files"
                description="Split categories into their own files. When off, everything goes to localmind.log."
                control={
                  <Switch
                    checked={useSeparateLogFiles}
                    onChange={(useSeparateLogFiles) =>
                      setDiagnostics({ useSeparateLogFiles })
                    }
                    aria-label="Separate log files"
                  />
                }
              />
              {useSeparateLogFiles ? (
                <>
                  <SettingRow
                    nested
                    title="Error log file"
                    description="errors.log"
                    control={
                      <Switch
                        checked={draft.diagnostics.enableErrorLogs}
                        onChange={(enableErrorLogs) =>
                          setDiagnostics({ enableErrorLogs })
                        }
                        aria-label="Error log file"
                      />
                    }
                  />
                  <SettingRow
                    nested
                    title="Diagnostic events file"
                    description="advanced-events.ndjson"
                    control={
                      <Switch
                        checked={draft.diagnostics.enableDiagnosticEventLogs}
                        onChange={(enableDiagnosticEventLogs) =>
                          setDiagnostics({ enableDiagnosticEventLogs })
                        }
                        aria-label="Diagnostic events file"
                      />
                    }
                  />
                </>
              ) : null}
              <SettingRow
                title="Debug trace file"
                description="debug-trace.ndjson"
                control={
                  <Switch
                    checked={draft.diagnostics.enableDebugTrace}
                    onChange={(enableDebugTrace) =>
                      setDiagnostics({ enableDebugTrace })
                    }
                    aria-label="Debug trace file"
                  />
                }
              />
            </SettingGroup>

            <SettingGroup title="Log maintenance">
              <SettingRow
                title="Keep logs for N days"
                description="Older daily log files are removed automatically on a daily sweep."
                control={
                  <Input
                    type="number"
                    min={1}
                    max={365}
                    className="w-24"
                    value={draft.diagnostics.logRetainedDays}
                    onChange={(event) =>
                      setDiagnostics({
                        logRetainedDays: Number(event.target.value),
                      })
                    }
                  />
                }
              />
              <SettingRow
                title="Clear logs now"
                description="Delete current log files from the logs folder. Files in use are skipped."
                control={
                  <Button
                    variant="secondary"
                    disabled={isClearingLogs}
                    onClick={() => setClearLogsConfirmOpen(true)}
                  >
                    {isClearingLogs ? "Clearing…" : "Clear logs"}
                  </Button>
                }
              />
            </SettingGroup>

            <p className="text-xs leading-relaxed text-muted-foreground">
              By default LocalMind writes to localmind.log only. Separate files
              add errors.log, http.log, sql.log, advanced-events.ndjson, or
              debug-trace.ndjson when their matching switches are enabled. Log
              level changes apply without restart; changing the logs path may
              require restarting LocalMind before new files move there.
            </p>
          </div>
        ) : null}
      </Section>

      <Section
        id="watched-folders"
        title="Watched folders"
        description="Automatically create ingestion jobs when files are created, updated, or deleted in selected folders."
      >
        <div className="space-y-4">
          <div className="grid gap-3 md:grid-cols-3">
            <Field
              label="Auto-ingestion"
              description="Queue ingestion jobs automatically when watched files change."
            >
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
            </Field>

            <Field
              label="Debounce milliseconds"
              description="Wait time after a change before queuing, to batch rapid edits."
            >
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
            </Field>

            <Field
              label="Delete policy"
              description="What happens when a watched file is removed from disk."
            >
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
            </Field>
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

                  <div className="mt-3 flex flex-col gap-2 border-t border-border pt-3">
                    <div className="flex flex-wrap items-center gap-2 text-xs">
                      <span
                        className={`rounded-full px-2 py-0.5 font-medium ${
                          status?.healthStatus === "Active"
                            ? "bg-green-500/10 text-green-500"
                            : status?.healthStatus === "Missing" ||
                                status?.healthStatus === "WatcherError"
                              ? "bg-destructive/10 text-destructive"
                              : "bg-muted text-muted-foreground"
                        }`}
                      >
                        {status?.healthStatus || "Unknown"}
                      </span>
                      <span className="text-muted-foreground">
                        Active documents: {status?.activeDocumentsCount ?? 0}
                      </span>
                      {(status?.deletedWaitingCleanupCount ?? 0) > 0 && (
                        <span className="text-destructive font-medium">
                          Pending cleanup: {status?.deletedWaitingCleanupCount}
                        </span>
                      )}
                      <span className="text-muted-foreground">
                        Pending events: {status?.pendingEvents ?? 0}
                      </span>
                    </div>

                    {(status?.lastScanStartedAt ||
                      status?.lastScanCompletedAt) && (
                      <div className="text-xs text-muted-foreground">
                        <span className="mr-3">Last scan:</span>
                        {status.lastScanCompletedAt ? (
                          <span>
                            {new Date(
                              status.lastScanCompletedAt,
                            ).toLocaleString()}{" "}
                            ({status.lastScanNewFiles} queued,{" "}
                            {status.lastScanDeletedFiles} missing,{" "}
                            {status.lastScanUnchangedFiles} unchanged,{" "}
                            {status.lastScanUnsupportedFiles} unsupported)
                          </span>
                        ) : (
                          <span>
                            Started at{" "}
                            {status.lastScanStartedAt
                              ? new Date(
                                  status.lastScanStartedAt,
                                ).toLocaleString()
                              : "Unknown"}{" "}
                            (Scanning...)
                          </span>
                        )}
                      </div>
                    )}

                    {status?.lastError ? (
                      <div className="text-destructive text-xs">
                        Error: {status.lastError}
                      </div>
                    ) : null}
                  </div>

                  <div className="mt-3 flex items-center justify-between">
                    <button
                      className="text-destructive text-sm font-medium hover:underline"
                      type="button"
                      onClick={() => removeWatchedFolder(index)}
                    >
                      Remove folder
                    </button>
                    <Button
                      variant="secondary"
                      disabled={
                        rescanningPath === folder.path || !folder.enabled
                      }
                      onClick={() => handleRescan(folder.path)}
                    >
                      {rescanningPath === folder.path
                        ? "Rescanning..."
                        : "Rescan"}
                    </Button>
                  </div>
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

          <div className="flex flex-wrap items-center gap-3 pt-2">
            <Button
              variant="secondary"
              disabled={isRescanningAll}
              onClick={() => handleRescan()}
            >
              {isRescanningAll ? "Rescanning..." : "Rescan All"}
            </Button>
            <Button
              variant="secondary"
              onClick={() => setCleanupConfirmOpen(true)}
            >
              Cleanup deleted files
            </Button>
          </div>
        </div>
      </Section>

      <ConfirmDialog
        open={isCleanupConfirmOpen}
        title="Cleanup deleted watched files?"
        description="This will permanently remove internal application data for files that have been deleted from your watched folders. Original physical files will not be affected."
        confirmLabel="Cleanup"
        isPending={isCleaning}
        onConfirm={handleCleanup}
        onClose={() => setCleanupConfirmOpen(false)}
      />

      <ConfirmDialog
        open={isClearLogsConfirmOpen}
        title="Clear log files?"
        description="This deletes current log files from the logs folder. Files currently in use are skipped. This does not affect your documents or knowledge base."
        confirmLabel="Clear logs"
        isPending={isClearingLogs}
        onConfirm={handleClearLogs}
        onClose={() => setClearLogsConfirmOpen(false)}
      />

      <Toast
        message={toast?.message ?? null}
        variant={toast?.variant}
        onDismiss={dismissToast}
      />
    </div>
  );
}

function Section({
  id,
  title,
  description,
  badge,
  children,
}: {
  id: string;
  title: string;
  description: string;
  badge?: ReactNode;
  children: ReactNode;
}) {
  return (
    <section className="rounded-xl border border-border bg-card p-5" id={id}>
      <div className="flex items-center gap-3">
        <h2 className="text-lg font-semibold text-foreground">{title}</h2>
        {badge}
      </div>
      <p className="mt-1 text-sm text-muted-foreground">{description}</p>
      <div className="mt-4">{children}</div>
    </section>
  );
}

function SettingGroup({
  title,
  children,
}: {
  title: string;
  children: ReactNode;
}) {
  return (
    <div className="space-y-2">
      <p className="px-1 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
        {title}
      </p>
      <div className="divide-y divide-border rounded-lg border border-border">
        {children}
      </div>
    </div>
  );
}

function SettingRow({
  title,
  description,
  control,
  nested = false,
}: {
  title: string;
  description?: string;
  control: ReactNode;
  nested?: boolean;
}) {
  return (
    <div
      className={cn(
        "flex items-center justify-between gap-4 px-4 py-3",
        nested && "pl-10",
      )}
    >
      <div className="min-w-0">
        <p className="text-sm font-medium text-foreground">{title}</p>
        {description ? (
          <p className="mt-0.5 text-xs text-muted-foreground">{description}</p>
        ) : null}
      </div>
      <div className="shrink-0">{control}</div>
    </div>
  );
}

function Field({
  label,
  description,
  children,
}: {
  label: string;
  description?: string;
  children: ReactNode;
}) {
  return (
    <label className="space-y-1 text-sm font-medium text-foreground">
      <span className="block">{label}</span>
      {description ? (
        <span className="block text-xs font-normal text-muted-foreground">
          {description}
        </span>
      ) : null}
      {children}
    </label>
  );
}

function TextField({
  label,
  description,
  value,
  onChange,
}: {
  label: string;
  description?: string;
  value: string;
  onChange: (value: string) => void;
}) {
  return (
    <Field label={label} description={description}>
      <Input value={value} onChange={(event) => onChange(event.target.value)} />
    </Field>
  );
}

function PathField({
  label,
  description,
  value,
  onChange,
  kind,
}: {
  label: string;
  description?: string;
  value: string;
  onChange: (value: string) => void;
  kind: "folder" | "file";
}) {
  async function handleBrowse() {
    try {
      const picked = await pickFolder();
      if (picked) {
        onChange(picked);
      }
    } catch (error) {
      console.error(error);
    }
  }

  function reveal() {
    if (!value) {
      return;
    }
    const action =
      kind === "folder"
        ? openPathInExplorer(value)
        : revealFileInExplorer(value);
    void action.catch((error) => console.error(error));
  }

  const buttonClass =
    "flex h-11 w-11 shrink-0 items-center justify-center rounded-md border border-border bg-card text-muted-foreground transition hover:bg-muted hover:text-foreground";

  return (
    <Field label={label} description={description}>
      <div className="flex gap-2">
        <Input
          className="flex-1 font-mono"
          value={value}
          title={
            value
              ? `${value}\nRight-click to open in Explorer`
              : "Right-click to open in Explorer"
          }
          onChange={(event) => onChange(event.target.value)}
          onContextMenu={(event) => {
            event.preventDefault();
            reveal();
          }}
        />
        {kind === "folder" ? (
          <button
            type="button"
            className={buttonClass}
            title="Choose folder"
            aria-label="Choose folder"
            onClick={handleBrowse}
          >
            <FolderOpen size={16} aria-hidden />
          </button>
        ) : (
          <button
            type="button"
            className={buttonClass}
            title="Show in Explorer"
            aria-label="Show in Explorer"
            onClick={reveal}
          >
            <ExternalLink size={16} aria-hidden />
          </button>
        )}
      </div>
    </Field>
  );
}
