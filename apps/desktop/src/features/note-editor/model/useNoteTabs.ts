import { useCallback, useState } from "react";
import type { NoteDto } from "@entities/note";
import type { OpenNoteTab } from "./types";

type UseNoteTabsOptions = {
  onConfirmCloseDirtyTab: (noteId: string, title: string) => Promise<boolean>;
  onConfirmReplaceDirtyTab: (noteId: string, title: string) => Promise<boolean>;
};

type OpenTabDecision =
  | { type: "focus"; noteId: string }
  | { type: "add" }
  | {
      type: "replace";
      activeTabId: string;
      requiresConfirmation: boolean;
      title: string;
    };

type CloseTabDecision =
  | { type: "missing" }
  | { type: "close"; requiresConfirmation: boolean; title: string };

export function getOpenTabDecision(
  openTabs: OpenNoteTab[],
  activeTabId: string | null,
  note: NoteDto,
  options: { preferNewTab?: boolean } = {},
): OpenTabDecision {
  if (openTabs.some((tab) => tab.noteId === note.id)) {
    return { type: "focus", noteId: note.id };
  }

  if (options.preferNewTab || !activeTabId) {
    return { type: "add" };
  }

  const activeTab = openTabs.find((tab) => tab.noteId === activeTabId);
  if (!activeTab) {
    return { type: "add" };
  }

  return {
    type: "replace",
    activeTabId,
    requiresConfirmation: activeTab.isDirty,
    title: activeTab.title,
  };
}

export function getCloseTabDecision(
  openTabs: OpenNoteTab[],
  noteId: string,
): CloseTabDecision {
  const tab = openTabs.find((item) => item.noteId === noteId);
  if (!tab) {
    return { type: "missing" };
  }

  return {
    type: "close",
    requiresConfirmation: tab.isDirty,
    title: tab.title,
  };
}

export function useNoteTabs({
  onConfirmCloseDirtyTab,
  onConfirmReplaceDirtyTab,
}: UseNoteTabsOptions) {
  const [openTabs, setOpenTabs] = useState<OpenNoteTab[]>([]);
  const [activeTabId, setActiveTabId] = useState<string | null>(null);

  const openTab = useCallback(
    async (note: NoteDto) => {
      const decision = getOpenTabDecision(openTabs, activeTabId, note);
      if (decision.type === "focus") {
        setActiveTabId(decision.noteId);
      } else if (decision.type === "add") {
        setOpenTabs((prev) => [
          ...prev,
          { noteId: note.id, title: note.title, isDirty: false },
        ]);
        setActiveTabId(note.id);
      } else {
        if (decision.requiresConfirmation) {
          const canReplace = await onConfirmReplaceDirtyTab(
            decision.activeTabId,
            decision.title,
          );
          if (!canReplace) {
            return;
          }
        }

        setOpenTabs((prev) =>
          prev.map((tab) =>
            tab.noteId === decision.activeTabId
              ? { noteId: note.id, title: note.title, isDirty: false }
              : tab,
          ),
        );
        setActiveTabId(note.id);
      }
    },
    [activeTabId, onConfirmReplaceDirtyTab, openTabs],
  );

  const openInNewTab = useCallback(
    (note: NoteDto) => {
      const decision = getOpenTabDecision(openTabs, activeTabId, note, {
        preferNewTab: true,
      });
      if (decision.type === "focus") {
        setActiveTabId(decision.noteId);
        return;
      }

      setOpenTabs((prev) => {
        if (prev.some((tab) => tab.noteId === note.id)) {
          return prev;
        }
        return [
          ...prev,
          { noteId: note.id, title: note.title, isDirty: false },
        ];
      });
      setActiveTabId(note.id);
    },
    [activeTabId, openTabs],
  );

  const closeTab = useCallback(
    async (noteId: string) => {
      const decision = getCloseTabDecision(openTabs, noteId);
      if (decision.type === "missing") return;

      if (decision.requiresConfirmation) {
        const canClose = await onConfirmCloseDirtyTab(noteId, decision.title);
        if (!canClose) {
          return;
        }
      }

      setOpenTabs((prev) => {
        const next = prev.filter((t) => t.noteId !== noteId);
        setActiveTabId((currentActive) => {
          if (currentActive === noteId) {
            return next.length > 0 ? next[next.length - 1].noteId : null;
          }
          return currentActive;
        });
        return next;
      });
    },
    [onConfirmCloseDirtyTab, openTabs],
  );

  const forceCloseTab = useCallback((noteId: string) => {
    setOpenTabs((prev) => {
      const next = prev.filter((t) => t.noteId !== noteId);
      setActiveTabId((currentActive) => {
        if (currentActive === noteId) {
          return next.length > 0 ? next[next.length - 1].noteId : null;
        }
        return currentActive;
      });
      return next;
    });
  }, []);

  const setTabDirty = useCallback((noteId: string, isDirty: boolean) => {
    setOpenTabs((prev) =>
      prev.map((t) => (t.noteId === noteId ? { ...t, isDirty } : t)),
    );
  }, []);

  const updateTabTitle = useCallback((noteId: string, title: string) => {
    setOpenTabs((prev) =>
      prev.map((t) => (t.noteId === noteId ? { ...t, title } : t)),
    );
  }, []);

  return {
    openTabs,
    activeTabId,
    setActiveTabId,
    openTab,
    openInNewTab,
    closeTab,
    forceCloseTab,
    setTabDirty,
    updateTabTitle,
  };
}
