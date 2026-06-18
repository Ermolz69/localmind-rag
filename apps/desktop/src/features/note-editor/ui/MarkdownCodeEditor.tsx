import { useEffect, useRef } from "react";
import { history, historyKeymap, indentWithTab } from "@codemirror/commands";
import { markdown as markdownLanguage } from "@codemirror/lang-markdown";
import { bracketMatching, syntaxHighlighting } from "@codemirror/language";
import { defaultHighlightStyle } from "@codemirror/language";
import { EditorState, RangeSetBuilder } from "@codemirror/state";
import {
  Decoration,
  type DecorationSet,
  EditorView,
  keymap,
  ViewPlugin,
  type ViewUpdate,
  WidgetType,
} from "@codemirror/view";
import type { EditorViewMode } from "../model/types";

type MarkdownCodeEditorProps = {
  markdown: string;
  mode: Extract<EditorViewMode, "source" | "live-preview">;
  onMarkdownChange: (markdown: string) => void;
};

class BulletWidget extends WidgetType {
  toDOM() {
    const bullet = document.createElement("span");
    bullet.className = "cm-md-list-bullet";
    bullet.textContent = "*";
    return bullet;
  }
}

class CheckboxWidget extends WidgetType {
  constructor(
    private readonly checked: boolean,
    private readonly from: number,
    private readonly to: number,
  ) {
    super();
  }

  eq(other: CheckboxWidget) {
    return (
      other.checked === this.checked &&
      other.from === this.from &&
      other.to === this.to
    );
  }

  toDOM(view: EditorView) {
    const checkbox = document.createElement("button");
    checkbox.type = "button";
    checkbox.className = this.checked
      ? "cm-md-checkbox cm-md-checkbox-checked"
      : "cm-md-checkbox";
    checkbox.textContent = this.checked ? "x" : "";
    checkbox.setAttribute("aria-pressed", String(this.checked));
    checkbox.addEventListener("pointerdown", (event) => {
      event.preventDefault();
      event.stopPropagation();
    });
    checkbox.addEventListener("mousedown", (event) => {
      event.preventDefault();
      event.stopPropagation();
    });
    checkbox.addEventListener("click", (event) => {
      event.preventDefault();
      event.stopPropagation();
      view.dispatch({
        changes: {
          from: this.from,
          to: this.to,
          insert: this.checked ? "[ ]" : "[x]",
        },
      });
    });
    return checkbox;
  }

  ignoreEvent() {
    return true;
  }
}

class EmptyWidget extends WidgetType {
  toDOM() {
    const span = document.createElement("span");
    span.className = "cm-md-empty-widget";
    return span;
  }
}

class HorizontalRuleWidget extends WidgetType {
  toDOM() {
    const rule = document.createElement("hr");
    rule.className = "cm-md-horizontal-rule";
    return rule;
  }
}

class TableRowWidget extends WidgetType {
  constructor(
    private readonly cells: string[],
    private readonly isHeader: boolean,
  ) {
    super();
  }

  eq(other: TableRowWidget) {
    return (
      other.isHeader === this.isHeader &&
      other.cells.length === this.cells.length &&
      other.cells.every((cell, index) => cell === this.cells[index])
    );
  }

  toDOM() {
    const row = document.createElement("span");
    row.className = this.isHeader ? "cm-md-table-row cm-md-table-header" : "cm-md-table-row";

    for (const cellText of this.cells) {
      const cell = document.createElement("span");
      cell.className = "cm-md-table-cell";
      cell.textContent = cellText.trim();
      row.appendChild(cell);
    }

    return row;
  }
}

class ImageWidget extends WidgetType {
  constructor(
    private readonly alt: string,
    private readonly src: string,
  ) {
    super();
  }

  eq(other: ImageWidget) {
    return other.alt === this.alt && other.src === this.src;
  }

  toDOM() {
    const container = document.createElement("span");
    container.className = "cm-md-image-widget";

    const image = document.createElement("img");
    image.className = "cm-md-image-preview";
    image.src = this.src;
    image.alt = this.alt;
    image.loading = "lazy";
    image.referrerPolicy = "no-referrer";
    image.addEventListener("error", () => {
      container.replaceChildren();
      const fallback = document.createElement("a");
      fallback.className = "cm-md-image-fallback";
      fallback.href = this.src;
      fallback.textContent = this.alt || this.src;
      fallback.target = "_blank";
      fallback.rel = "noreferrer";
      container.appendChild(fallback);
    });

    container.appendChild(image);
    return container;
  }
}

function intersects(from: number, to: number, activeFrom: number, activeTo: number) {
  return from <= activeTo && to >= activeFrom;
}

type ActiveRange = { from: number; to: number } | null;

type PendingDecoration = {
  from: number;
  to: number;
  decoration: Decoration;
};

type HighlightPart = {
  text: string;
  className: string;
  from: number;
  to: number;
};

const jsKeywordPattern =
  /\b(?:async|await|break|case|catch|class|const|continue|default|do|else|export|extends|finally|for|from|function|if|import|in|let|new|return|switch|throw|try|typeof|var|while|yield)\b/g;

const jsTokenPattern =
  /("(?:\\.|[^"\\])*"|'(?:\\.|[^'\\])*'|`(?:\\.|[^`\\])*`|\/\/.*$|\b\d+(?:\.\d+)?\b|\b(?:true|false|null|undefined)\b)/g;

function highlightCodeLine(code: string, language: string | null): HighlightPart[] {
  const normalizedLanguage = language?.toLowerCase() ?? "";
  if (
    normalizedLanguage &&
    !["js", "jsx", "ts", "tsx", "javascript", "typescript"].includes(
      normalizedLanguage,
    )
  ) {
    return [];
  }

  const parts: HighlightPart[] = [];
  let cursor = 0;

  for (const match of code.matchAll(jsTokenPattern)) {
    const from = match.index;
    const text = match[0];
    if (from > cursor) {
      parts.push(...highlightKeywords(code.slice(cursor, from), cursor));
    }

    parts.push({
      text,
      className: tokenClassName(text),
      from,
      to: from + text.length,
    });
    cursor = from + text.length;
  }

  if (cursor < code.length) {
    parts.push(...highlightKeywords(code.slice(cursor), cursor));
  }

  return parts;
}

function highlightKeywords(text: string, offset: number): HighlightPart[] {
  const parts: HighlightPart[] = [];
  let cursor = 0;

  for (const match of text.matchAll(jsKeywordPattern)) {
    const from = match.index;
    if (from > cursor) {
      parts.push({
        text: text.slice(cursor, from),
        className: "cm-md-code-token",
        from: offset + cursor,
        to: offset + from,
      });
    }
    parts.push({
      text: match[0],
      className: "cm-md-code-keyword",
      from: offset + from,
      to: offset + from + match[0].length,
    });
    cursor = from + match[0].length;
  }

  if (cursor < text.length) {
    parts.push({
      text: text.slice(cursor),
      className: "cm-md-code-token",
      from: offset + cursor,
      to: offset + text.length,
    });
  }

  return parts;
}

function addCodeDecorations(
  builder: RangeSetBuilder<Decoration>,
  lineFrom: number,
  text: string,
  language: string | null,
) {
  builder.add(lineFrom, lineFrom, Decoration.line({ class: "cm-md-code-line" }));

  for (const part of highlightCodeLine(text, language)) {
    if (part.className === "cm-md-code-token" || part.from === part.to) {
      continue;
    }

    builder.add(
      lineFrom + part.from,
      lineFrom + part.to,
      Decoration.mark({ class: part.className }),
    );
  }
}

function tokenClassName(token: string) {
  if (token.startsWith("//")) return "cm-md-code-comment";
  if (/^["'`]/.test(token)) return "cm-md-code-string";
  if (/^\d/.test(token)) return "cm-md-code-number";
  if (/^(?:true|false|null|undefined)$/.test(token)) {
    return "cm-md-code-atom";
  }
  return "cm-md-code-token";
}

function activeMarkdownBlock(view: EditorView) {
  if (!view.hasFocus) {
    return null;
  }

  const head = view.state.selection.main.head;
  const doc = view.state.doc;
  const activeLine = doc.lineAt(head);
  let fenceStart: number | null = null;

  for (let lineNumber = 1; lineNumber <= doc.lines; lineNumber += 1) {
    const line = doc.line(lineNumber);
    if (/^\s*```/.test(line.text)) {
      if (fenceStart === null) {
        fenceStart = line.from;
      } else {
        const blockFrom = fenceStart;
        const blockTo = line.to;
        if (head >= blockFrom && head <= blockTo) {
          return { from: blockFrom, to: blockTo };
        }
        fenceStart = null;
      }
    }
  }

  return { from: activeLine.from, to: activeLine.to };
}

function addHiddenRange(
  decorations: PendingDecoration[],
  from: number,
  to: number,
  active: ActiveRange,
) {
  if (from >= to || (active && intersects(from, to, active.from, active.to))) {
    return;
  }

  decorations.push({
    from,
    to,
    decoration: Decoration.mark({ class: "cm-md-hidden-syntax" }),
  });
}

function addCollapsedRange(
  builder: RangeSetBuilder<Decoration>,
  from: number,
  to: number,
  active: ActiveRange,
) {
  if (from >= to || (active && intersects(from, to, active.from, active.to))) {
    return;
  }

  builder.add(
    from,
    to,
    Decoration.replace({ widget: new EmptyWidget() }),
  );
}

function replaceLineWithWidget(
  builder: RangeSetBuilder<Decoration>,
  from: number,
  to: number,
  widget: WidgetType,
) {
  builder.add(from, to, Decoration.replace({ widget }));
}

function parseTableCells(text: string) {
  const trimmed = text.trim();
  if (!trimmed.includes("|")) return null;

  const normalized = trimmed.startsWith("|") ? trimmed.slice(1) : trimmed;
  const withoutTrailing = normalized.endsWith("|")
    ? normalized.slice(0, -1)
    : normalized;
  const cells = withoutTrailing.split("|").map((cell) => cell.trim());
  return cells.length > 1 ? cells : null;
}

function isTableSeparator(text: string) {
  const cells = parseTableCells(text);
  return Boolean(cells?.every((cell) => /^:?-{3,}:?$/.test(cell)));
}

function addInlineDecorations(
  builder: RangeSetBuilder<Decoration>,
  lineFrom: number,
  text: string,
  active: ActiveRange,
) {
  const decorations: PendingDecoration[] = [];

  const boldPattern = /\*\*([^*]+)\*\*/g;
  for (const match of text.matchAll(boldPattern)) {
    const start = lineFrom + match.index;
    const contentStart = start + 2;
    const contentEnd = contentStart + match[1].length;
    const end = contentEnd + 2;
    if (active && intersects(start, end, active.from, active.to)) continue;
    addHiddenRange(decorations, start, contentStart, active);
    decorations.push({
      from: contentStart,
      to: contentEnd,
      decoration: Decoration.mark({ class: "cm-md-bold" }),
    });
    addHiddenRange(decorations, contentEnd, end, active);
  }

  const italicPattern = /(?<!\*)\*([^*\n]+)\*(?!\*)/g;
  for (const match of text.matchAll(italicPattern)) {
    const start = lineFrom + match.index;
    const contentStart = start + 1;
    const contentEnd = contentStart + match[1].length;
    const end = contentEnd + 1;
    if (active && intersects(start, end, active.from, active.to)) continue;
    addHiddenRange(decorations, start, contentStart, active);
    decorations.push({
      from: contentStart,
      to: contentEnd,
      decoration: Decoration.mark({ class: "cm-md-italic" }),
    });
    addHiddenRange(decorations, contentEnd, end, active);
  }

  const underscoreItalicPattern = /(?<!_)_([^_\n]+)_(?!_)/g;
  for (const match of text.matchAll(underscoreItalicPattern)) {
    const start = lineFrom + match.index;
    const contentStart = start + 1;
    const contentEnd = contentStart + match[1].length;
    const end = contentEnd + 1;
    if (active && intersects(start, end, active.from, active.to)) continue;
    addHiddenRange(decorations, start, contentStart, active);
    decorations.push({
      from: contentStart,
      to: contentEnd,
      decoration: Decoration.mark({ class: "cm-md-italic" }),
    });
    addHiddenRange(decorations, contentEnd, end, active);
  }

  const inlineCodePattern = /`([^`\n]+)`/g;
  for (const match of text.matchAll(inlineCodePattern)) {
    const start = lineFrom + match.index;
    const contentStart = start + 1;
    const contentEnd = contentStart + match[1].length;
    const end = contentEnd + 1;
    if (active && intersects(start, end, active.from, active.to)) continue;
    addHiddenRange(decorations, start, contentStart, active);
    decorations.push({
      from: contentStart,
      to: contentEnd,
      decoration: Decoration.mark({ class: "cm-md-inline-code" }),
    });
    addHiddenRange(decorations, contentEnd, end, active);
  }

  const strikethroughPattern = /~~([^~\n]+)~~/g;
  for (const match of text.matchAll(strikethroughPattern)) {
    const start = lineFrom + match.index;
    const contentStart = start + 2;
    const contentEnd = contentStart + match[1].length;
    const end = contentEnd + 2;
    if (active && intersects(start, end, active.from, active.to)) continue;
    addHiddenRange(decorations, start, contentStart, active);
    decorations.push({
      from: contentStart,
      to: contentEnd,
      decoration: Decoration.mark({ class: "cm-md-strikethrough" }),
    });
    addHiddenRange(decorations, contentEnd, end, active);
  }

  const imagePattern = /!\[([^\]]*)\]\(([^)]+)\)/g;
  for (const match of text.matchAll(imagePattern)) {
    const start = lineFrom + match.index;
    const labelStart = start + 2;
    const labelEnd = labelStart + match[1].length;
    const end = start + match[0].length;
    if (active && intersects(start, end, active.from, active.to)) continue;
    addHiddenRange(decorations, start, labelStart, active);
    decorations.push({
      from: labelStart,
      to: labelEnd,
      decoration: Decoration.mark({ class: "cm-md-image" }),
    });
    addHiddenRange(decorations, labelEnd, end, active);
  }

  const linkPattern = /(?<!!)\[([^\]]+)\]\(([^)]+)\)/g;
  for (const match of text.matchAll(linkPattern)) {
    const start = lineFrom + match.index;
    const labelStart = start + 1;
    const labelEnd = labelStart + match[1].length;
    const end = start + match[0].length;
    if (active && intersects(start, end, active.from, active.to)) continue;
    addHiddenRange(decorations, start, labelStart, active);
    decorations.push({
      from: labelStart,
      to: labelEnd,
      decoration: Decoration.mark({ class: "cm-md-link" }),
    });
    addHiddenRange(decorations, labelEnd, end, active);
  }

  const wikiLinkPattern = /\[\[([^\]]+)\]\]/g;
  for (const match of text.matchAll(wikiLinkPattern)) {
    const start = lineFrom + match.index;
    const labelStart = start + 2;
    const labelEnd = labelStart + match[1].length;
    const end = labelEnd + 2;
    if (active && intersects(start, end, active.from, active.to)) continue;
    addHiddenRange(decorations, start, labelStart, active);
    decorations.push({
      from: labelStart,
      to: labelEnd,
      decoration: Decoration.mark({ class: "cm-md-wiki-link" }),
    });
    addHiddenRange(decorations, labelEnd, end, active);
  }

  decorations
    .sort((left, right) => left.from - right.from || left.to - right.to)
    .forEach(({ from, to, decoration }) => {
      builder.add(from, to, decoration);
    });
}

function livePreviewDecorations(view: EditorView) {
  const builder = new RangeSetBuilder<Decoration>();
  const active = activeMarkdownBlock(view);
  let inFence = false;
  let fenceLanguage: string | null = null;

  for (let lineNumber = 1; lineNumber <= view.state.doc.lines; lineNumber += 1) {
    const line = view.state.doc.line(lineNumber);
    const text = line.text;
    const activeLine = active
      ? intersects(line.from, line.to, active.from, active.to)
      : false;
    const fenceMatch = text.match(/^(\s*```.*)$/);
    const tableCells = parseTableCells(text);

    if (fenceMatch) {
      if (!activeLine) {
        replaceLineWithWidget(builder, line.from, line.to, new EmptyWidget());
      }
      if (inFence) {
        inFence = false;
        fenceLanguage = null;
      } else {
        inFence = true;
        fenceLanguage = text.trim().slice(3).trim() || null;
      }
      continue;
    }

    if (inFence) {
      if (!activeLine) {
        addCodeDecorations(builder, line.from, text, fenceLanguage);
      }
      continue;
    }

    if (!activeLine && /^\s*([-*_])(?:\s*\1){2,}\s*$/.test(text)) {
      replaceLineWithWidget(
        builder,
        line.from,
        line.to,
        new HorizontalRuleWidget(),
      );
      continue;
    }

    const blockImageMatch = text.match(/^\s*!\[([^\]]*)\]\(([^)]+)\)\s*$/);
    if (!activeLine && blockImageMatch) {
      replaceLineWithWidget(
        builder,
        line.from,
        line.to,
        new ImageWidget(blockImageMatch[1], blockImageMatch[2]),
      );
      continue;
    }

    if (!activeLine && tableCells) {
      if (isTableSeparator(text)) {
        replaceLineWithWidget(builder, line.from, line.to, new EmptyWidget());
        continue;
      }

      const nextLine =
        lineNumber < view.state.doc.lines
          ? view.state.doc.line(lineNumber + 1).text
          : "";
      replaceLineWithWidget(
        builder,
        line.from,
        line.to,
        new TableRowWidget(tableCells, isTableSeparator(nextLine)),
      );
      continue;
    }

    const headingMatch = text.match(/^(#{1,6})(\s+)(.*)$/);
    if (headingMatch && !activeLine) {
      const level = Math.min(headingMatch[1].length, 6);
      const contentFrom =
        line.from + headingMatch[1].length + headingMatch[2].length;
      addCollapsedRange(
        builder,
        line.from,
        contentFrom,
        active,
      );
      builder.add(
        contentFrom,
        line.to,
        Decoration.mark({
          class: `cm-md-heading-text cm-md-heading-${level}`,
        }),
      );
    }

    const quoteMatch = text.match(/^(\s*>+\s?)/);
    if (quoteMatch && !activeLine) {
      builder.add(
        line.from,
        line.from,
        Decoration.line({ class: "cm-md-quote-line" }),
      );
      builder.add(
        line.from,
        line.from + quoteMatch[1].length,
        Decoration.mark({ class: "cm-md-hidden-syntax" }),
      );
    }

    const checkboxMatch = text.match(/^(\s*[-*]\s+\[([ xX])\]\s+)/);
    if (checkboxMatch && !activeLine) {
      const checkboxStart = line.from + checkboxMatch[1].indexOf("[");
      builder.add(
        line.from,
        line.from + checkboxMatch[1].length,
        Decoration.replace({
          widget: new CheckboxWidget(
            checkboxMatch[2].toLowerCase() === "x",
            checkboxStart,
            checkboxStart + 3,
          ),
        }),
      );
    } else {
      const listMatch = text.match(/^(\s*[-*]\s+)/);
      if (listMatch && !activeLine) {
        builder.add(
          line.from,
          line.from + listMatch[1].length,
          Decoration.replace({ widget: new BulletWidget() }),
        );
      }
    }

    if (!activeLine) {
      addInlineDecorations(builder, line.from, text, active);
    }
  }

  return builder.finish();
}

const livePreviewExtension = ViewPlugin.fromClass(
  class {
    decorations: DecorationSet;

    constructor(view: EditorView) {
      this.decorations = livePreviewDecorations(view);
    }

    update(update: ViewUpdate) {
      if (update.docChanged || update.selectionSet || update.viewportChanged) {
        this.decorations = livePreviewDecorations(update.view);
      }
    }
  },
  {
    decorations: (plugin) => plugin.decorations,
  },
);

const editorTheme = EditorView.theme({
  "&": {
    height: "100%",
    backgroundColor: "hsl(var(--card))",
    color: "hsl(var(--foreground))",
  },
  ".cm-scroller": {
    fontFamily: "ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace",
    fontSize: "15px",
    lineHeight: "1.75",
    padding: "1.25rem 1.5rem 5rem",
  },
  ".cm-content": {
    caretColor: "hsl(var(--foreground))",
    color: "hsl(var(--foreground))",
  },
  ".cm-line": {
    color: "hsl(var(--foreground))",
    paddingLeft: "0",
    textIndent: "0",
  },
  ".cm-focused": {
    outline: "none",
  },
  ".cm-selectionBackground, &.cm-focused .cm-selectionBackground": {
    backgroundColor: "hsl(var(--primary) / 0.2)",
  },
  ".cm-md-heading-text": {
    fontFamily: "inherit",
    fontWeight: "700",
  },
  ".cm-md-heading-1": { fontSize: "1.875rem", lineHeight: "2.25rem" },
  ".cm-md-heading-2": { fontSize: "1.5rem", lineHeight: "2rem" },
  ".cm-md-heading-3": { fontSize: "1.25rem", lineHeight: "1.75rem" },
  ".cm-md-heading-4": { fontSize: "1.125rem", lineHeight: "1.75rem" },
  ".cm-md-heading-5": { fontSize: "1rem", lineHeight: "1.5rem" },
  ".cm-md-heading-6": { fontSize: "0.875rem", lineHeight: "1.25rem" },
  ".cm-md-bold": {
    fontWeight: "700",
  },
  ".cm-md-italic": {
    fontStyle: "italic",
  },
  ".cm-md-strikethrough": {
    textDecoration: "line-through",
  },
  ".cm-md-link, .cm-md-wiki-link": {
    color: "hsl(var(--primary))",
    textDecoration: "underline",
    textUnderlineOffset: "0.2em",
  },
  ".cm-md-image": {
    color: "hsl(var(--primary))",
    textDecoration: "underline",
    textUnderlineOffset: "0.2em",
  },
  ".cm-md-image-widget": {
    display: "inline-block",
    maxWidth: "100%",
  },
  ".cm-md-image-preview": {
    display: "block",
    maxWidth: "100%",
    maxHeight: "22rem",
    borderRadius: "0.375rem",
    border: "1px solid hsl(var(--border))",
    objectFit: "contain",
  },
  ".cm-md-image-fallback": {
    color: "hsl(var(--primary))",
    textDecoration: "underline",
    textUnderlineOffset: "0.2em",
  },
  ".cm-md-inline-code": {
    borderRadius: "0.25rem",
    backgroundColor: "hsl(var(--muted))",
    padding: "0.05rem 0.25rem",
    fontFamily: "ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace",
  },
  ".cm-md-quote-line": {
    borderLeft: "0.1875rem solid hsl(var(--primary))",
    color: "hsl(var(--muted-foreground))",
    paddingLeft: "0.75rem",
  },
  ".cm-md-list-bullet": {
    display: "inline-block",
    width: "1.25rem",
    color: "hsl(var(--muted-foreground))",
  },
  ".cm-md-checkbox": {
    display: "inline-flex",
    width: "0.9rem",
    height: "0.9rem",
    marginRight: "0.35rem",
    alignItems: "center",
    justifyContent: "center",
    border: "1px solid hsl(var(--border))",
    borderRadius: "0.1875rem",
    backgroundColor: "transparent",
    color: "hsl(var(--primary))",
    cursor: "pointer",
    fontSize: "0.75rem",
    lineHeight: "1",
    padding: "0",
  },
  ".cm-md-code-line": {
    backgroundColor: "hsl(var(--muted))",
    fontFamily: "ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace",
  },
  ".cm-md-code-token": {
    color: "hsl(var(--foreground))",
  },
  ".cm-md-code-keyword": {
    color: "hsl(var(--primary))",
    fontWeight: "700",
  },
  ".cm-md-code-string": {
    color: "hsl(var(--primary))",
  },
  ".cm-md-code-number, .cm-md-code-atom": {
    color: "hsl(var(--accent-foreground))",
  },
  ".cm-md-code-comment": {
    color: "hsl(var(--muted-foreground))",
    fontStyle: "italic",
  },
  ".cm-md-fence": {
    color: "hsl(var(--muted-foreground))",
  },
  ".cm-md-horizontal-rule": {
    display: "block",
    width: "100%",
    border: "0",
    borderTop: "1px solid hsl(var(--border))",
    margin: "0.75rem 0",
  },
  ".cm-md-table-row": {
    display: "inline-flex",
    maxWidth: "100%",
    overflow: "hidden",
    borderLeft: "1px solid hsl(var(--border))",
    borderTop: "1px solid hsl(var(--border))",
    verticalAlign: "middle",
  },
  ".cm-md-table-cell": {
    display: "inline-block",
    minWidth: "7rem",
    borderRight: "1px solid hsl(var(--border))",
    borderBottom: "1px solid hsl(var(--border))",
    padding: "0.15rem 0.5rem",
  },
  ".cm-md-table-header .cm-md-table-cell": {
    backgroundColor: "hsl(var(--muted))",
    fontWeight: "700",
  },
  ".cm-md-hidden-syntax": {
    color: "transparent",
    opacity: "0",
  },
  ".cm-md-empty-widget": {
    display: "inline-block",
    width: "0",
    overflow: "hidden",
  },
});

function editorExtensions(
  mode: MarkdownCodeEditorProps["mode"],
  onMarkdownChange: (markdown: string) => void,
) {
  return [
    history(),
    bracketMatching(),
    markdownLanguage(),
    mode === "source"
      ? syntaxHighlighting(defaultHighlightStyle, { fallback: true })
      : [],
    keymap.of([indentWithTab, ...historyKeymap]),
    EditorView.lineWrapping,
    EditorView.updateListener.of((update) => {
      if (update.docChanged) {
        onMarkdownChange(update.state.doc.toString());
      }
    }),
    editorTheme,
    mode === "live-preview" ? livePreviewExtension : [],
  ];
}

export function MarkdownCodeEditor({
  markdown,
  mode,
  onMarkdownChange,
}: MarkdownCodeEditorProps) {
  const containerRef = useRef<HTMLDivElement | null>(null);
  const viewRef = useRef<EditorView | null>(null);
  const onMarkdownChangeRef = useRef(onMarkdownChange);
  const markdownRef = useRef(markdown);
  onMarkdownChangeRef.current = onMarkdownChange;
  markdownRef.current = markdown;

  useEffect(() => {
    if (!containerRef.current) return;

    const view = new EditorView({
      parent: containerRef.current,
      state: EditorState.create({
        doc: markdownRef.current,
        extensions: editorExtensions(mode, (value) =>
          onMarkdownChangeRef.current(value),
        ),
      }),
    });

    viewRef.current = view;
    return () => {
      view.destroy();
      viewRef.current = null;
    };
  }, [mode]);

  useEffect(() => {
    const view = viewRef.current;
    if (!view) return;

    const current = view.state.doc.toString();
    if (current === markdown) return;

    view.dispatch({
      changes: { from: 0, to: current.length, insert: markdown },
    });
  }, [markdown]);

  return <div ref={containerRef} className="h-full min-h-0" />;
}
