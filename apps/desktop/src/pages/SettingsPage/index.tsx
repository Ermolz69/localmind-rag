import { themes } from "../../shared/theme/tokens";
import { useTheme } from "../../shared/theme/theme-provider";

export function SettingsPage() {
  const { theme, setTheme } = useTheme();

  return (
    <section className="space-y-4">
      <div>
        <h1 className="text-2xl font-semibold">Settings</h1>
        <p className="text-sm text-muted-foreground">
          Local runtime, AI provider, sync, and theme settings.
        </p>
      </div>
      <div className="rounded-md border border-border bg-card p-4">
        <label className="text-sm font-medium" htmlFor="theme">
          Theme
        </label>
        <select
          id="theme"
          className="mt-2 h-10 rounded-md border border-border bg-background px-3 text-sm"
          value={theme}
          onChange={(event) => setTheme(event.target.value as typeof theme)}
        >
          {themes.map((item) => (
            <option key={item} value={item}>
              {item}
            </option>
          ))}
        </select>
      </div>
    </section>
  );
}
