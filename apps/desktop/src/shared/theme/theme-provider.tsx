import {
  createContext,
  useContext,
  useEffect,
  useCallback,
  useMemo,
  useState,
  type PropsWithChildren,
} from "react";
import { isThemeName, resolveTheme } from "./themes";
import { themeClassNames, type ThemeName } from "./tokens";

type ThemeContextValue = {
  resolvedTheme: "light" | "dark";
  theme: ThemeName;
  setTheme: (theme: ThemeName) => void;
  toggleTheme: () => void;
};

const ThemeContext = createContext<ThemeContextValue | null>(null);
const currentThemeStorageKey = "localmind.theme.current";
const lastLightThemeStorageKey = "localmind.theme.lastLight";
const lastDarkThemeStorageKey = "localmind.theme.lastDark";

function readStoredTheme(key: string, fallback: ThemeName): ThemeName {
  const storedTheme = window.localStorage.getItem(key);

  return storedTheme && isThemeName(storedTheme) ? storedTheme : fallback;
}

export function ThemeProvider({ children }: PropsWithChildren) {
  const [theme, setThemeState] = useState<ThemeName>(() =>
    readStoredTheme(currentThemeStorageKey, "system"),
  );
  const [lastLightTheme, setLastLightTheme] = useState<ThemeName>(() =>
    readStoredTheme(lastLightThemeStorageKey, "light"),
  );
  const [lastDarkTheme, setLastDarkTheme] = useState<ThemeName>(() =>
    readStoredTheme(lastDarkThemeStorageKey, "dark"),
  );
  const resolvedTheme = resolveTheme(theme);

  useEffect(() => {
    const root = document.documentElement;

    root.classList.toggle("dark", resolvedTheme === "dark");
    Object.values(themeClassNames).forEach((className) => {
      root.classList.remove(className);
    });

    const themeClassName = themeClassNames[theme];
    if (themeClassName) {
      root.classList.add(themeClassName);
    }
  }, [resolvedTheme, theme]);

  const setTheme = useCallback((nextTheme: ThemeName) => {
    const nextResolvedTheme = resolveTheme(nextTheme);

    setThemeState(nextTheme);
    window.localStorage.setItem(currentThemeStorageKey, nextTheme);

    if (nextResolvedTheme === "dark") {
      setLastDarkTheme(nextTheme);
      window.localStorage.setItem(lastDarkThemeStorageKey, nextTheme);
    } else {
      setLastLightTheme(nextTheme);
      window.localStorage.setItem(lastLightThemeStorageKey, nextTheme);
    }
  }, []);

  const toggleTheme = useCallback(() => {
    setTheme(resolvedTheme === "dark" ? lastLightTheme : lastDarkTheme);
  }, [lastDarkTheme, lastLightTheme, resolvedTheme, setTheme]);

  const value = useMemo(
    () => ({ resolvedTheme, theme, setTheme, toggleTheme }),
    [resolvedTheme, setTheme, theme, toggleTheme],
  );

  return (
    <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>
  );
}

export function useTheme() {
  const context = useContext(ThemeContext);
  if (!context) {
    throw new Error("useTheme must be used inside ThemeProvider.");
  }

  return context;
}
