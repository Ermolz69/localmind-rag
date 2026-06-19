export const themes = [
  "light",
  "dark",
  "system",
  "graphite-blue",
  "midnight-violet",
  "slate-teal-amber",
  "carbon-gray-blue",
] as const;
export type ThemeName = (typeof themes)[number];

export const themeClassNames: Partial<Record<ThemeName, string>> = {
  "graphite-blue": "theme-graphite-blue",
  "midnight-violet": "theme-midnight-violet",
  "slate-teal-amber": "theme-slate-teal",
  "carbon-gray-blue": "theme-carbon-gray",
};

export const darkAlternativeThemes = [
  "graphite-blue",
  "midnight-violet",
  "slate-teal-amber",
  "carbon-gray-blue",
] as const satisfies readonly ThemeName[];

export const lightAlternativeThemes =
  [] as const satisfies readonly ThemeName[];
