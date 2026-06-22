import {
  Circle,
  Layers,
  Monitor,
  Moon,
  Palette,
  RefreshCw,
  Save,
  Sparkles,
  Sun,
} from "lucide-react";
import { SettingsSections, useSettingsForm } from "@features/settings-form";
import {
  darkAlternativeThemes,
  themes,
  type ThemeName,
} from "@shared/theme/tokens";
import { Button, ErrorBanner, Modal, PageHeader, Skeleton } from "@shared/ui";

const themeLabels: Record<ThemeName, string> = {
  light: "Light",
  dark: "Dark",
  system: "System",
  "graphite-blue": "Graphite Blue",
  "midnight-violet": "Midnight Violet",
  "slate-teal-amber": "Slate Teal Amber",
  "carbon-gray-blue": "Carbon Gray Blue",
};

const themeIcons = {
  light: Sun,
  dark: Moon,
  system: Monitor,
  "graphite-blue": Layers,
  "midnight-violet": Sparkles,
  "slate-teal-amber": Palette,
  "carbon-gray-blue": Circle,
};
const darkAlternativeThemeSet = new Set<ThemeName>(darkAlternativeThemes);

const baseSettingsNavigation = [
  { href: "#appearance", label: "Appearance" },
  { href: "#ai", label: "AI" },
  { href: "#sync", label: "Sync" },
  { href: "#companion-mode", label: "Companion Mode" },
  { href: "#diagnostics", label: "Diagnostics" },
  { href: "#watched-folders", label: "Watched folders" },
];

export function SettingsPage() {
  const page = useSettingsForm();
  const settingsNavigation =
    (page.draft?.diagnostics.developerModeEnabled ?? false)
      ? [
          baseSettingsNavigation[0],
          { href: "#runtime-paths", label: "Runtime paths" },
          ...baseSettingsNavigation.slice(1),
        ]
      : baseSettingsNavigation;

  return (
    <div className="space-y-6">
      <PageHeader
        title="Settings"
        description="Configure local runtime, AI models, sync, watched folders, and diagnostics."
        actions={
          <div className="flex gap-2">
            <Button variant="secondary" onClick={() => void page.load()}>
              <RefreshCw className="h-4 w-4" />
              Refresh
            </Button>
            <Button
              variant="secondary"
              onClick={() => page.setThemeModalOpen(true)}
            >
              <Palette className="h-4 w-4" />
              Theme
            </Button>
          </div>
        }
      />

      {page.error ? <ErrorBanner message={page.error} /> : null}

      <div className="grid gap-6 lg:grid-cols-[220px_1fr]">
        <aside className="sticky top-6 self-start rounded-xl border border-border bg-card p-4">
          <p className="text-sm font-semibold text-foreground">On this page</p>
          <nav className="mt-3 space-y-2">
            {settingsNavigation.map((item) => (
              <a
                className="block rounded-md px-2 py-1 text-sm text-muted-foreground hover:bg-muted hover:text-foreground"
                href={item.href}
                key={item.href}
              >
                {item.label}
              </a>
            ))}
          </nav>
        </aside>

        <main className="space-y-6">
          <section
            className="rounded-xl border border-border bg-card p-5"
            id="appearance"
          >
            <h2 className="text-lg font-semibold text-foreground">
              Appearance
            </h2>
            <p className="mt-1 text-sm text-muted-foreground">
              Current theme: {themeLabels[page.theme]}
            </p>
            <Button
              className="mt-4"
              variant="secondary"
              onClick={() => page.setThemeModalOpen(true)}
            >
              Change
            </Button>
          </section>

          {page.isLoading ? (
            <div className="space-y-6">
              <Skeleton className="h-[200px] w-full" />
              <Skeleton className="h-[200px] w-full" />
              <Skeleton className="h-[200px] w-full" />
            </div>
          ) : page.draft ? (
            <>
              <SettingsSections
                draft={page.draft}
                watchedFolderStatus={page.watchedFolderStatus}
                onChange={page.setDraft}
              />

              <div className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-border bg-card p-5">
                <div>
                  <p className="font-medium text-foreground">
                    {page.isDirty
                      ? "Unsaved settings changes"
                      : "Settings are saved"}
                  </p>
                  <p className="text-sm text-muted-foreground">
                    {page.isDirty
                      ? "Save when the current setup looks right."
                      : "This panel stays ready for the next edit."}
                  </p>
                </div>
                <div className="flex gap-2">
                  <Button
                    disabled={!page.isDirty}
                    variant="secondary"
                    onClick={page.resetSettings}
                  >
                    Cancel
                  </Button>
                  <Button
                    disabled={page.isSaving || !page.isDirty}
                    onClick={() => void page.saveSettings()}
                  >
                    <Save className="h-4 w-4" />
                    {page.isSaving ? "Saving..." : "Save settings"}
                  </Button>
                </div>
              </div>
            </>
          ) : null}
        </main>
      </div>

      <Modal
        open={page.themeModalOpen}
        title="Choose theme"
        onClose={() => page.setThemeModalOpen(false)}
      >
        <div className="grid gap-2">
          {themes.map((item) => {
            const Icon = themeIcons[item];
            const selected = page.theme === item;
            const isDarkAlternative = darkAlternativeThemeSet.has(item);

            return (
              <button
                className={`flex items-center justify-between gap-3 rounded-md border px-3 py-2 text-sm ${
                  selected
                    ? "border-primary bg-primary/10 text-primary"
                    : "border-border hover:bg-muted"
                }`}
                key={item}
                type="button"
                onClick={() => {
                  page.setTheme(item);
                  page.setThemeModalOpen(false);
                }}
              >
                <span className="flex min-w-0 items-center gap-2">
                  <Icon className="h-4 w-4 shrink-0" />
                  <span className="truncate">{themeLabels[item]}</span>
                </span>
                {isDarkAlternative ? (
                  <span className="shrink-0 text-xs text-muted-foreground">
                    Dark alternative
                  </span>
                ) : null}
              </button>
            );
          })}
        </div>
      </Modal>
    </div>
  );
}
