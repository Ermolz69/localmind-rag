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

export function SettingsPage() {
  const page = useSettingsForm();

  return (
    <section className="space-y-5">
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

      <section className="rounded-md border border-border bg-card p-5">
        <div className="flex items-center justify-between gap-4">
          <div>
            <h2 className="text-base font-semibold text-card-foreground">
              Appearance
            </h2>
            <p className="mt-1 text-sm text-muted-foreground">
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
        <div className="rounded-md border border-border bg-card p-5 text-sm text-muted-foreground">
          Loading settings...
        </div>
      ) : page.draft ? (
        <>
          <SettingsSections draft={page.draft} onChange={page.setDraft} />
          <div className="flex gap-2">
            <Button
              onClick={() => void page.saveSettings()}
              disabled={page.isSaving || !page.isDirty}
            >
              <Save size={16} aria-hidden />
              {page.isSaving ? "Saving..." : "Save settings"}
            </Button>
            <Button
              variant="secondary"
              onClick={page.resetSettings}
              disabled={!page.isDirty}
            >
              Cancel
            </Button>
          </div>
        </>
      ) : null}

      <DiagnosticsPanel diagnostics={page.diagnostics} />

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
