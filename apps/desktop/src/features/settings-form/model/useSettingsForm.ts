import { useEffect, useState } from "react";
import type {
  AppSettings,
  WatchedFolderStatusResponse,
} from "@entities/settings";
import { clearDiagnosticsCache } from "@features/diagnostics";
import { toAppSettings, toAppSettingsDto } from "@entities/settings";
import { getFieldErrors, settingsApi, watchedFoldersApi } from "@shared/api";
import { useApiMutation, useApiQuery } from "@shared/lib/hooks";
import { useTheme } from "@shared/theme/theme-provider";
import { fromApiThemeName, toApiThemeName } from "@shared/theme/themes";
import type { ThemeName } from "@shared/theme/tokens";

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
        settingsApi.getSettings().then(toAppSettings),
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
    (nextSettings: AppSettings) =>
      settingsApi.saveSettings(toAppSettingsDto(nextSettings)),
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

      window.dispatchEvent(new CustomEvent("localmind:settings:changed"));

      if (!draft.diagnostics.enabled) {
        void clearDiagnosticsCache();
      }
    } else if (saveMutation.rawError) {
      setFieldErrors(getFieldErrors(saveMutation.rawError));
    }
  }

  function resetSettings() {
    setDraftState(settings);
    setHasLocalChanges(false);

    if (settings) {
      setTheme(fromApiThemeName(settings.appearance.theme));
    }
  }

  function setSelectedTheme(nextTheme: ThemeName) {
    setTheme(nextTheme);

    setDraftState((currentDraft) => {
      if (!currentDraft) {
        return currentDraft;
      }

      const nextDraft: AppSettings = {
        ...currentDraft,
        appearance: {
          ...currentDraft.appearance,
          theme: toApiThemeName(nextTheme),
        },
      };

      setHasLocalChanges(
        JSON.stringify(settings) !== JSON.stringify(nextDraft),
      );
      return nextDraft;
    });
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
    setTheme: setSelectedTheme,
    setThemeModalOpen,
    theme,
    themeModalOpen,
    watchedFolderStatus:
      data?.watchedFolderStatus ?? (null as WatchedFolderStatusResponse | null),
  };
}
