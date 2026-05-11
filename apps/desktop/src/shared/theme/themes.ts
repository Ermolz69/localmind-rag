import type { ThemeName } from "./tokens";

export function resolveTheme(theme: ThemeName): "light" | "dark" {
  if (theme !== "system") {
    return theme;
  }

  return window.matchMedia("(prefers-color-scheme: dark)").matches
    ? "dark"
    : "light";
}
