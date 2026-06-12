import type { BucketDto } from "@entities/bucket";
import type { DocumentSummary } from "@entities/document";
import type { RetrievalFilters } from "@entities/search";

export type ParseCommandResult = {
  content: string;
  filters: RetrievalFilters;
  consumedCommand: boolean;
  error: string | null;
};

export type LiveDraftFilterResult = {
  content: string;
  filters: RetrievalFilters;
  consumedCommand: boolean;
};

const fileTypeAliases = new Set([
  "pdf",
  "docx",
  "pptx",
  "markdown",
  "md",
  "txt",
  "html",
]);

export function extractLiveCommands(
  rawInput: string,
  activeFilters: RetrievalFilters,
  buckets: BucketDto[],
  documents: DocumentSummary[],
): LiveDraftFilterResult {
  const nextFilters: RetrievalFilters = { ...activeFilters };
  const contentLines: string[] = [];
  let consumedCommand = false;

  for (const line of rawInput.split(/\r?\n/)) {
    const trimmed = line.trim();

    if (!trimmed.startsWith("/")) {
      contentLines.push(line);
      continue;
    }

    if (tryApplyKnownCommand(trimmed, nextFilters, buckets, documents)) {
      consumedCommand = true;
      continue;
    }

    contentLines.push(line);
  }

  return {
    content: contentLines.join("\n"),
    filters: nextFilters,
    consumedCommand,
  };
}

export type AutocompleteSuggestion = {
  text: string;
  description?: string;
};

export type AutocompleteContext = {
  suggestions: AutocompleteSuggestion[];
  query: string;
  type:
    | "command"
    | "bucket"
    | "file-type"
    | "date"
    | "document"
    | "tag"
    | "none";
  startIndex: number;
  endIndex: number;
};

export function getAutocompleteContext(
  rawInput: string,
  cursorPosition: number,
  buckets: BucketDto[],
  documents: DocumentSummary[],
): AutocompleteContext {
  const defaultContext: AutocompleteContext = {
    suggestions: [],
    query: "",
    type: "none",
    startIndex: 0,
    endIndex: 0,
  };

  if (cursorPosition < 0 || cursorPosition > rawInput.length) {
    return defaultContext;
  }

  const lines = rawInput.split("\n");
  let lineStartIndex = 0;
  let activeLineIndex = -1;

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];
    const lineEndIndex = lineStartIndex + line.length;
    if (
      cursorPosition >= lineStartIndex &&
      cursorPosition <= lineEndIndex + 1
    ) {
      activeLineIndex = i;
      break;
    }
    lineStartIndex = lineEndIndex + 1;
  }

  if (activeLineIndex === -1) {
    return defaultContext;
  }

  const line = lines[activeLineIndex] || "";
  const relativeCursorPos = cursorPosition - lineStartIndex;
  const prefix = line.slice(0, relativeCursorPos);

  if (!prefix.trimStart().startsWith("/")) {
    return defaultContext;
  }

  const trimmedStartOffset = prefix.length - prefix.trimStart().length;
  // Use a regex that handles quotes so `/document "My File"` works better,
  // but for simple completion we just split by space for the command.
  const parts = prefix.trimStart().split(/\s+/);
  const commandWord = parts[0] || "";

  if (parts.length === 1) {
    const query = commandWord;
    const allCommands: AutocompleteSuggestion[] = [
      { text: "/bucket", description: "Filter by bucket" },
      { text: "/date", description: "Filter by document date" },
      { text: "/document", description: "Filter by document name" },
      { text: "/file-type", description: "Filter by file format" },
      { text: "/tag", description: "Filter by tag (key=value)" },
    ];
    const filtered = allCommands.filter((c) =>
      c.text.toLowerCase().startsWith(query.toLowerCase()),
    );

    const startIndex = lineStartIndex + trimmedStartOffset;
    const endIndex = lineStartIndex + prefix.length;

    return {
      suggestions: filtered,
      query,
      type: "command",
      startIndex,
      endIndex,
    };
  }

  const commandName = commandWord.toLowerCase();
  const argStart = prefix.indexOf(commandWord) + commandWord.length;
  const argPrefix = prefix.slice(argStart);
  const argQuery = argPrefix.trimStart();
  const argOffset = argPrefix.length - argPrefix.trimStart().length;

  const startIndex = lineStartIndex + argStart + argOffset;
  const endIndex = lineStartIndex + prefix.length;

  if (commandName === "/bucket") {
    const allBuckets = buckets.map((b) => ({
      text: b.name,
      description: "Bucket filter",
    }));
    const filtered = allBuckets.filter((b) =>
      b.text.toLowerCase().includes(argQuery.toLowerCase()),
    );

    return {
      suggestions: filtered,
      query: argQuery,
      type: "bucket",
      startIndex,
      endIndex,
    };
  }

  if (commandName === "/document" || commandName === "/doc") {
    // Note: To support spaces in document names when parsing, users should quote them or we take the rest of the line.
    // We will take the rest of the line as the document name.
    const allDocs = documents.map((d) => ({
      text: `"${d.name}"`,
      description: "Document filter",
    }));

    // For filtering, we strip out quotes if they typed them
    const cleanQuery = argQuery.replace(/^"|"$/g, "").toLowerCase();

    const filtered = allDocs.filter((d) =>
      d.text.toLowerCase().includes(cleanQuery),
    );

    return {
      suggestions: filtered,
      query: argQuery,
      type: "document",
      startIndex,
      endIndex,
    };
  }

  if (commandName === "/file-type") {
    const allFileTypes = Array.from(fileTypeAliases).map((t) => ({
      text: t,
      description: "File extension filter",
    }));
    const filtered = allFileTypes.filter((t) =>
      t.text.toLowerCase().startsWith(argQuery.toLowerCase()),
    );

    return {
      suggestions: filtered,
      query: argQuery,
      type: "file-type",
      startIndex,
      endIndex,
    };
  }

  if (commandName === "/date") {
    const allDateTemplates = [
      { text: "from=DD.MM.YYYY", description: "Filter documents after date" },
      { text: "to=DD.MM.YYYY", description: "Filter documents before date" },
      {
        text: "from=DD.MM.YYYY&to=DD.MM.YYYY",
        description: "Filter documents within date range",
      },
    ];
    const filtered = allDateTemplates.filter((d) =>
      d.text.toLowerCase().includes(argQuery.toLowerCase()),
    );

    return {
      suggestions: filtered,
      query: argQuery,
      type: "date",
      startIndex,
      endIndex,
    };
  }

  if (commandName === "/tag") {
    // No specific catalog to autocomplete tags right now.
    return {
      suggestions: [],
      query: argQuery,
      type: "tag",
      startIndex,
      endIndex,
    };
  }

  return defaultContext;
}

export function prepareChatSubmission(
  rawInput: string,
  activeFilters: RetrievalFilters,
  buckets: BucketDto[],
  documents: DocumentSummary[],
): ParseCommandResult {
  const nextFilters: RetrievalFilters = { ...activeFilters };
  const contentLines: string[] = [];
  let consumedCommand = false;

  for (const line of rawInput.split(/\r?\n/)) {
    const trimmed = line.trim();

    if (!trimmed.startsWith("/")) {
      contentLines.push(line);
      continue;
    }

    const parsed = parseCommandStrict(trimmed, nextFilters, buckets, documents);
    if (parsed.error) {
      return {
        content: rawInput.trim(),
        filters: activeFilters,
        consumedCommand: false,
        error: parsed.error,
      };
    }

    consumedCommand = true;
  }

  return {
    content: contentLines.join("\n").trim(),
    filters: nextFilters,
    consumedCommand,
    error: null,
  };
}

function tryApplyKnownCommand(
  commandLine: string,
  filters: RetrievalFilters,
  buckets: BucketDto[],
  documents: DocumentSummary[],
): boolean {
  const parsed = parseCommandStrict(commandLine, filters, buckets, documents);
  return parsed.error === null;
}

function parseCommandStrict(
  commandLine: string,
  filters: RetrievalFilters,
  buckets: BucketDto[],
  documents: DocumentSummary[],
): { error: string | null } {
  const [command, ...parts] = commandLine.split(/\s+/);
  const value = parts.join(" ").trim();
  const commandLower = command.toLowerCase();

  if (commandLower === "/bucket") {
    return parseBucketCommandStrict(value, filters, buckets);
  }

  if (commandLower === "/date") {
    return parseDateCommandStrict(value, filters);
  }

  if (commandLower === "/file-type") {
    return parseFileTypeCommandStrict(value, filters);
  }

  if (commandLower === "/document" || commandLower === "/doc") {
    return parseDocumentCommandStrict(value, filters, documents);
  }

  if (commandLower === "/tag") {
    return parseTagCommandStrict(value, filters);
  }

  return { error: `Unsupported chat filter command: ${command}.` };
}

function parseBucketCommandStrict(
  bucketName: string,
  filters: RetrievalFilters,
  buckets: BucketDto[],
): { error: string | null } {
  if (!bucketName) {
    return { error: "Use /bucket followed by a bucket name." };
  }

  const matches = buckets.filter(
    (bucket) => bucket.name.toLowerCase() === bucketName.toLowerCase(),
  );

  if (matches.length === 0) {
    return { error: `Bucket "${bucketName}" was not found.` };
  }

  if (matches.length > 1) {
    return { error: `Bucket "${bucketName}" matches more than one bucket.` };
  }

  filters.bucketId = matches[0].id;
  return { error: null };
}

function parseDocumentCommandStrict(
  documentName: string,
  filters: RetrievalFilters,
  documents: DocumentSummary[],
): { error: string | null } {
  if (!documentName) {
    return { error: "Use /document followed by a document name." };
  }

  // Remove surrounding quotes if present
  const cleanName = documentName.replace(/^"|"$/g, "");

  const matches = documents.filter(
    (doc) => doc.name.toLowerCase() === cleanName.toLowerCase(),
  );

  if (matches.length === 0) {
    return { error: `Document "${cleanName}" was not found.` };
  }

  if (matches.length > 1) {
    return {
      error: `Document "${cleanName}" matches more than one document. Please be more specific.`,
    };
  }

  filters.documentId = matches[0].id;
  return { error: null };
}

function parseTagCommandStrict(
  tagExpr: string,
  filters: RetrievalFilters,
): { error: string | null } {
  if (!tagExpr) {
    return { error: "Use /tag key=value." };
  }

  const equalIdx = tagExpr.indexOf("=");
  if (equalIdx === -1) {
    return { error: "Tag filter must be in the format key=value." };
  }

  const key = tagExpr.slice(0, equalIdx).trim();
  const value = tagExpr.slice(equalIdx + 1).trim();

  if (!key) {
    return { error: "Tag key cannot be empty." };
  }

  if (!filters.tags) {
    filters.tags = {};
  }

  filters.tags[key] = value;
  return { error: null };
}

function parseDateCommandStrict(
  commandValue: string,
  filters: RetrievalFilters,
): { error: string | null } {
  const params = new URLSearchParams(commandValue);
  const from = params.get("from");
  const to = params.get("to");

  if (!from && !to) {
    return { error: "Use /date with from=DD.MM.YYYY and/or to=DD.MM.YYYY." };
  }

  const dateFrom = from ? parseDate(from) : null;
  const dateTo = to ? parseDate(to) : null;

  if ((from && !dateFrom) || (to && !dateTo)) {
    return { error: "Date filters must use DD.MM.YYYY format." };
  }

  if (dateFrom && dateTo && dateFrom > dateTo) {
    return { error: "Date from must be before or equal to date to." };
  }

  filters.dateFrom = dateFrom;
  filters.dateTo = dateTo;
  return { error: null };
}

function parseFileTypeCommandStrict(
  fileType: string,
  filters: RetrievalFilters,
): { error: string | null } {
  const normalized = fileType.toLowerCase();

  if (!fileTypeAliases.has(normalized)) {
    return {
      error:
        "File type must be one of: pdf, docx, pptx, markdown, md, txt, html.",
    };
  }

  filters.fileType = normalized;
  return { error: null };
}

function parseDate(value: string): string | null {
  const match = /^(\d{2})\.(\d{2})\.(\d{4})$/.exec(value);
  if (!match) {
    return null;
  }

  const day = Number(match[1]);
  const month = Number(match[2]);
  const year = Number(match[3]);
  const date = new Date(Date.UTC(year, month - 1, day));

  if (
    date.getUTCFullYear() !== year ||
    date.getUTCMonth() !== month - 1 ||
    date.getUTCDate() !== day
  ) {
    return null;
  }

  return date.toISOString();
}
