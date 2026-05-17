import { useCallback, useEffect, useState } from "react";
import type { AppSettings } from "@entities/settings";
import type { DiagnosticsStatus } from "@entities/runtime";
import { diagnosticsApi, getErrorMessage, settingsApi } from "@shared/api";
import { useTheme } from "@shared/theme/theme-provider";

export function useSettingsForm() {
  const { theme, setTheme } = useTheme();
  const [settings, setSettings] = useState<AppSettings | null>(null);
  const [draft, setDraft] = useState<AppSettings | null>(null);
  const [diagnostics, setDiagnostics] = useState<DiagnosticsStatus | null>(
    null,
  );
  const [themeModalOpen, setThemeModalOpen] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setError(null);
    setIsLoading(true);
    try {
      const [nextSettings, nextDiagnostics] = await Promise.all([
        settingsApi.getSettings(),
        diagnosticsApi.getDiagnostics(),
      ]);
      setSettings(nextSettings);
      setDraft(nextSettings);
      setDiagnostics(nextDiagnostics);
    } catch (exception) {
      setError(getErrorMessage(exception, "Unable to load settings."));
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  async function saveSettings() {
    if (!draft) {
      return;
    }

    setError(null);
    setIsSaving(true);
    try {
      await settingsApi.saveSettings(draft);
      setSettings(draft);
    } catch (exception) {
      setError(getErrorMessage(exception, "Unable to save settings."));
    } finally {
      setIsSaving(false);
    }
  }

  function resetSettings() {
    setDraft(settings);
  }

  return {
    diagnostics,
    draft,
    error,
    isDirty: JSON.stringify(settings) !== JSON.stringify(draft),
    isLoading,
    isSaving,
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
