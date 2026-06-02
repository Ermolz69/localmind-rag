import { useEffect, useState } from "react";
import type { AppSettings } from "@entities/settings";
import { diagnosticsApi, getFieldErrors, settingsApi } from "@shared/api";
import { useApiMutation, useApiQuery } from "@shared/lib/hooks";
import { useTheme } from "@shared/theme/theme-provider";

export function useSettingsForm() {
  const { theme, setTheme } = useTheme();
  const [settings, setSettings] = useState<AppSettings | null>(null);
  const [draft, setDraft] = useState<AppSettings | null>(null);
  const [themeModalOpen, setThemeModalOpen] = useState(false);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({});

  const {
    data,
    isLoading,
    error: queryError,
    reload: load,
  } = useApiQuery(
    async () => {
      const [nextSettings, nextDiagnostics] = await Promise.all([
        settingsApi.getSettings(),
        diagnosticsApi.getDiagnostics(),
      ]);
      return { settings: nextSettings, diagnostics: nextDiagnostics };
    },
    { fallbackError: "Unable to load settings." },
  );

  useEffect(() => {
    if (data) {
      setSettings(data.settings);
      setDraft(data.settings);
    }
  }, [data]);

  const saveMutation = useApiMutation(
    (nextSettings: AppSettings) => settingsApi.saveSettings(nextSettings),
    { fallbackError: "Unable to save settings." },
  );

  async function saveSettings() {
    if (!draft) {
      return;
    }

    setFieldErrors({});
    const success = await saveMutation.mutate(draft);
    if (success !== null) {
      setSettings(draft);
    } else if (saveMutation.rawError) {
      setFieldErrors(getFieldErrors(saveMutation.rawError));
    }
  }

  function resetSettings() {
    setDraft(settings);
  }

  return {
    diagnostics: data?.diagnostics ?? null,
    draft,
    error: queryError ?? saveMutation.error,
    fieldErrors,
    isDirty: JSON.stringify(settings) !== JSON.stringify(draft),
    isLoading,
    isSaving: saveMutation.isPending,
    load,
    resetSettings,
    saveSettings,
    setDraft,
    setTheme,
    setThemeModalOpen,
    theme,
    themeModalOpen,
  };
}
