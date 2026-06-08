import { useEffect, useState } from "react";
import type {
  AppSettings,
  WatchedFolderStatusResponse,
} from "@entities/settings";
import { getFieldErrors, settingsApi, watchedFoldersApi } from "@shared/api";
import { useApiMutation, useApiQuery } from "@shared/lib/hooks";
import { useTheme } from "@shared/theme/theme-provider";

export function useSettingsForm() {
  const { theme, setTheme } = useTheme();

  const [settings, setSettings] = useState<AppSettings | null>(null);
  const [draft, setDraftState] = useState<AppSettings | null>(null);
  const [hasLocalChanges, setHasLocalChanges] = useState(false);
  const [themeModalOpen, setThemeModalOpen] = useState(false);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({});

  const {
    data,
    isLoading,
    error: queryError,
    reload: load,
  } = useApiQuery(
    async () => {
      const [nextSettings, nextWatchedFolderStatus] = await Promise.all([
        settingsApi.getSettings(),
        watchedFoldersApi.getStatus().catch(() => null),
      ]);

      return {
        settings: nextSettings,
        watchedFolderStatus: nextWatchedFolderStatus,
      };
    },
    { fallbackError: "Unable to load settings." },
  );

  useEffect(() => {
    if (!data) {
      return;
    }

    setSettings(data.settings);

    setDraftState((currentDraft) => {
      if (hasLocalChanges && currentDraft) {
        return currentDraft;
      }

      return data.settings;
    });
  }, [data, hasLocalChanges]);

  const saveMutation = useApiMutation(
    (nextSettings: AppSettings) => settingsApi.saveSettings(nextSettings),
    { fallbackError: "Unable to save settings." },
  );

  const isDirty = JSON.stringify(settings) !== JSON.stringify(draft);

  function setDraft(nextDraft: AppSettings | null) {
    setDraftState(nextDraft);
    setHasLocalChanges(JSON.stringify(settings) !== JSON.stringify(nextDraft));
  }

  async function saveSettings() {
    if (!draft) {
      return;
    }

    setFieldErrors({});

    const success = await saveMutation.mutate(draft);

    if (success !== null) {
      setSettings(draft);
      setDraftState(draft);
      setHasLocalChanges(false);
    } else if (saveMutation.rawError) {
      setFieldErrors(getFieldErrors(saveMutation.rawError));
    }
  }

  function resetSettings() {
    setDraftState(settings);
    setHasLocalChanges(false);
  }

  return {
    draft,
    error: queryError ?? saveMutation.error,
    fieldErrors,
    isDirty,
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
    watchedFolderStatus:
      data?.watchedFolderStatus ?? (null as WatchedFolderStatusResponse | null),
  };
}
