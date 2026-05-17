import type { AppSettings } from "@entities/settings";
import type { ReactNode } from "react";
import { Input } from "@shared/ui";
import { Select } from "@shared/ui";

type SettingsSectionsProps = {
  draft: AppSettings;
  onChange: (settings: AppSettings) => void;
};

export function SettingsSections({ draft, onChange }: SettingsSectionsProps) {
  return (
    <div className="space-y-4">
      <Section
        id="runtime-paths"
        title="Runtime paths"
        description="Portable folders and local database locations used by the desktop runtime."
      >
        <div className="grid gap-4 lg:grid-cols-2">
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
        description="Provider, models, and local runtime executable settings."
      >
        <div className="grid gap-4 lg:grid-cols-2">
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
        description="Cloud sync toggles stay optional, so local offline work remains independent."
      >
        <div className="grid gap-4 lg:grid-cols-2">
          <label className="flex flex-col gap-2 text-sm">
            <span className="font-medium">Sync enabled</span>
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
          <label className="flex flex-col gap-2 text-sm">
            <span className="font-medium">Auto sync</span>
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
    <section
      id={id}
      className="scroll-mt-6 space-y-5 rounded-xl border border-border bg-card p-5 shadow-sm sm:p-6"
    >
      <div>
        <h2 className="text-base font-semibold text-card-foreground">
          {title}
        </h2>
        <p className="mt-1 text-sm leading-6 text-muted-foreground">
          {description}
        </p>
      </div>
      {children}
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
    <label className="flex flex-col gap-2 text-sm">
      <span className="font-medium">{label}</span>
      <Input value={value} onChange={(event) => onChange(event.target.value)} />
    </label>
  );
}
