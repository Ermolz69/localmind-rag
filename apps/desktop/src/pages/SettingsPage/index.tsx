import { Monitor, Moon, Palette, Sun } from "lucide-react";
import { useState } from "react";
import { themes, type ThemeName } from "../../shared/theme/tokens";
import { useTheme } from "../../shared/theme/theme-provider";
import { Button } from "../../shared/ui/Button";
import { Modal } from "../../shared/ui/Modal";

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
  const { theme, setTheme } = useTheme();
  const [themeModalOpen, setThemeModalOpen] = useState(false);

  return (
    <section className="space-y-5">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold">Settings</h1>
          <p className="text-sm text-muted-foreground">
            Local runtime, AI provider, sync, and visual preferences.
          </p>
        </div>
        <Button onClick={() => setThemeModalOpen(true)}>
          <Palette size={16} aria-hidden />
          Theme
        </Button>
      </div>

      <div className="rounded-md border border-border bg-card p-5">
        <div className="flex items-center justify-between gap-4">
          <div>
            <h2 className="text-base font-semibold text-card-foreground">
              Appearance
            </h2>
            <p className="mt-1 text-sm text-muted-foreground">
              Current theme: {themeLabels[theme]}
            </p>
          </div>
          <Button variant="secondary" onClick={() => setThemeModalOpen(true)}>
            Change
          </Button>
        </div>
      </div>

      <Modal
        open={themeModalOpen}
        title="Choose theme"
        description="Switch localmind between light, dark, or your system theme."
        onClose={() => setThemeModalOpen(false)}
      >
        <div className="grid gap-3 sm:grid-cols-3">
          {themes.map((item) => {
            const Icon = themeIcons[item];
            const selected = theme === item;
            return (
              <button
                key={item}
                className={`rounded-md border border-border p-4 text-left transition ${
                  selected
                    ? "bg-primary text-primary-foreground"
                    : "bg-background text-foreground hover:bg-muted"
                }`}
                onClick={() => {
                  setTheme(item);
                  setThemeModalOpen(false);
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
