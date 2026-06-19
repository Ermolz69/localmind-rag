import { lightAlternativeThemes, themes, type ThemeName } from "./tokens";

const themeSet = new Set<ThemeName>(themes);
const lightAlternativeThemeSet = new Set<ThemeName>(lightAlternativeThemes);

export function isThemeName(value: string): value is ThemeName {
  return themeSet.has(value as ThemeName);
}

export function resolveTheme(theme: ThemeName): "light" | "dark" {
  if (theme === "light" || lightAlternativeThemeSet.has(theme)) {
    return "light";
  }

  if (theme === "system") {
    return window.matchMedia("(prefers-color-scheme: dark)").matches
      ? "dark"
      : "light";
  }

  return "dark";
}

export function toApiThemeName(theme: ThemeName): string {
  switch (theme) {
    case "light":
      return "Light";
    case "dark":
      return "Dark";
    case "system":
      return "System";
    case "graphite-blue":
      return "GraphiteBlue";
    case "midnight-violet":
      return "MidnightViolet";
    case "slate-teal-amber":
      return "SlateTealAmber";
    case "carbon-gray-blue":
      return "CarbonGrayBlue";
  }
}

export function fromApiThemeName(theme: string): ThemeName {
  switch (theme.toLowerCase()) {
    case "light":
      return "light";
    case "dark":
      return "dark";
    case "graphiteblue":
      return "graphite-blue";
    case "midnightviolet":
      return "midnight-violet";
    case "slatetealamber":
      return "slate-teal-amber";
    case "carbongrayblue":
      return "carbon-gray-blue";
    case "system":
    default:
      return "system";
  }
}
