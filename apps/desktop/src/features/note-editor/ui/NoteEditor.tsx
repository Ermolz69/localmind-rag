import type { EditorViewMode, NoteDraft } from "../model/types";
import { MarkdownCodeEditor } from "./MarkdownCodeEditor";
import { MarkdownPreview } from "./MarkdownPreview";

type NoteEditorProps = {
  draft: NoteDraft;
  viewMode: EditorViewMode;
  onDraftChange: (draft: NoteDraft) => void;
};

export function NoteEditor({
  draft,
  viewMode,
  onDraftChange,
}: NoteEditorProps) {
  const showSource = viewMode === "source";

  return (
    <div className="flex h-full min-h-0 min-w-0 flex-col overflow-hidden bg-card">
      {showSource || viewMode === "live-preview" ? (
        <MarkdownCodeEditor
          markdown={draft.markdown}
          mode={showSource ? "source" : "live-preview"}
          onMarkdownChange={(markdown) => onDraftChange({ ...draft, markdown })}
        />
      ) : (
        <MarkdownPreview markdown={draft.markdown} />
      )}
    </div>
  );
}
