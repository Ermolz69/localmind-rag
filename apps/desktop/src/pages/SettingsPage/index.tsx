import { Monitor, Moon, Palette, RefreshCw, Save, Sun } from "lucide-react";
import {
  DiagnosticsPanel,
  SettingsSections,
  useSettingsForm,
} from "@features/settings-form";
import { themes, type ThemeName } from "@shared/theme/tokens";
import { Button, ErrorBanner, Modal, PageHeader } from "@shared/ui";

const themeLabels: Record<ThemeName, string> = {
  light: "Light",
  dark: "Dark",
  system: "System",
};

const themeIcons = {
  light: Sun,
  dark: Moon,
  system: Monitor,
};

const settingsNavigation = [
  { href: "#appearance", label: "Appearance" },
  { href: "#runtime-paths", label: "Runtime paths" },
  { href: "#ai", label: "AI" },
  { href: "#sync", label: "Sync" },
  { href: "#diagnostics", label: "Diagnostics" },
];

export function SettingsPage() {
  const page = useSettingsForm();

  return (
    <section className="mx-auto max-w-7xl space-y-5">
      <PageHeader
        title="Settings"
        description="Local runtime, AI provider, sync, diagnostics, and visual preferences."
        actions={
          <>
            <Button variant="secondary" onClick={() => void page.load()}>
              <RefreshCw size={16} aria-hidden />
              Refresh
            </Button>
            <Button onClick={() => page.setThemeModalOpen(true)}>
              <Palette size={16} aria-hidden />
              Theme
            </Button>
          </>
        }
      />

      <ErrorBanner message={page.error} />

      <div className="grid items-start gap-5 xl:grid-cols-[15rem_minmax(0,1fr)]">
        <aside className="sticky top-4 hidden self-start xl:block">
          <nav className="rounded-xl border border-border bg-card p-2 shadow-sm">
            <p className="px-3 py-2 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
              On this page
            </p>
            <div className="flex flex-col gap-1">
              {settingsNavigation.map((item) => (
                <a
                  key={item.href}
                  href={item.href}
                  className="rounded-md px-3 py-2 text-sm text-muted-foreground transition-colors hover:bg-muted hover:text-foreground"
                >
                  {item.label}
                </a>
              ))}
            </div>
          </nav>
        </aside>

        <div className="min-w-0 space-y-4">
          <section
            id="appearance"
            className="scroll-mt-6 rounded-xl border border-border bg-card p-5 shadow-sm sm:p-6"
          >
            <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
              <div>
                <h2 className="text-base font-semibold text-card-foreground">
                  Appearance
                </h2>
                <p className="mt-1 text-sm leading-6 text-muted-foreground">
                  Current theme: {themeLabels[page.theme]}
                </p>
              </div>
              <Button
                variant="secondary"
                onClick={() => page.setThemeModalOpen(true)}
              >
                Change
              </Button>
            </div>
          </section>

          {page.isLoading ? (
            <div className="rounded-xl border border-border bg-card p-5 text-sm text-muted-foreground shadow-sm sm:p-6">
              Loading settings...
            </div>
          ) : page.draft ? (
            <>
              <SettingsSections draft={page.draft} onChange={page.setDraft} />
              <DiagnosticsPanel diagnostics={page.diagnostics} />
              <div className="sticky bottom-4 z-10">
                <div className="rounded-xl border border-border bg-card/95 p-3 shadow-lg backdrop-blur">
                  <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                    <div>
                      <p className="text-sm font-medium text-card-foreground">
                        {page.isDirty
                          ? "Unsaved settings changes"
                          : "Settings are saved"}
                      </p>
                      <p className="text-xs leading-5 text-muted-foreground">
                        {page.isDirty
                          ? "Save when the current setup looks right."
                          : "This panel stays ready for the next edit."}
                      </p>
                    </div>
                    <div className="flex shrink-0 gap-2">
                      <Button
                        variant="secondary"
                        onClick={page.resetSettings}
                        disabled={!page.isDirty || page.isSaving}
                      >
                        Cancel
                      </Button>
                      <Button
                        onClick={() => void page.saveSettings()}
                        disabled={page.isSaving || !page.isDirty}
                      >
                        <Save size={16} aria-hidden />
                        {page.isSaving ? "Saving..." : "Save settings"}
                      </Button>
                    </div>
                  </div>
                </div>
              </div>
            </>
          ) : null}
        </div>
      </div>

      <Modal
        open={page.themeModalOpen}
        title="Choose theme"
        description="Switch localmind between light, dark, or your system theme."
        onClose={() => page.setThemeModalOpen(false)}
      >
        <div className="grid gap-3 sm:grid-cols-3">
          {themes.map((item) => {
            const Icon = themeIcons[item];
            const selected = page.theme === item;
            return (
              <button
                key={item}
                className={
                  selected
                    ? "rounded-md border border-border bg-primary p-4 text-left text-primary-foreground transition"
                    : "rounded-md border border-border bg-background p-4 text-left text-foreground transition hover:bg-muted"
                }
                onClick={() => {
                  page.setTheme(item);
                  page.setThemeModalOpen(false);
                }}
              >
                <Icon size={18} aria-hidden />
                <span className="mt-4 block text-sm font-medium">
                  {themeLabels[item]}
                </span>
              </button>
            );
          })}
        </div>
      </Modal>
    </section>
  );
}
