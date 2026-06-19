import type { Schema } from "@shared/contracts";

type DocumentPreviewResponse = Schema<"DocumentPreviewResponse">;

export function createTextPreviewResponse(
  overrides?: Partial<DocumentPreviewResponse>,
): DocumentPreviewResponse {
  return {
    documentId: crypto.randomUUID(),
    fileName: "fixture.txt",
    contentType: "text/plain; charset=utf-8",
    previewKind: "Text",
    textContent: "This is a plain text preview fixture.\nLine two.\n",
    previewUrl: null,
    errorCode: null,
    message: null,
    ...overrides,
  };
}

export function createMarkdownPreviewResponse(
  overrides?: Partial<DocumentPreviewResponse>,
): DocumentPreviewResponse {
  return {
    documentId: crypto.randomUUID(),
    fileName: "fixture.md",
    contentType: "text/markdown; charset=utf-8",
    previewKind: "Markdown",
    textContent: "# Test Document\n\nThis is a **markdown** preview fixture.\n",
    previewUrl: null,
    errorCode: null,
    message: null,
    ...overrides,
  };
}

export function createHtmlPreviewResponse(
  overrides?: Partial<DocumentPreviewResponse>,
): DocumentPreviewResponse {
  return {
    documentId: crypto.randomUUID(),
    fileName: "fixture.html",
    contentType: "text/html; charset=utf-8",
    previewKind: "Html",
    textContent:
      "<html><head><title>Test</title></head><body><p>HTML preview fixture.</p></body></html>",
    previewUrl: null,
    errorCode: null,
    message: null,
    ...overrides,
  };
}

export function createPdfPreviewResponse(
  overrides?: Partial<DocumentPreviewResponse>,
): DocumentPreviewResponse {
  const documentId = crypto.randomUUID();
  return {
    documentId,
    fileName: "fixture.pdf",
    contentType: "application/pdf",
    previewKind: "Pdf",
    previewUrl: `/api/v1/documents/${documentId}/preview/file`,
    textContent: null,
    errorCode: null,
    message: null,
    ...overrides,
  };
}

export function createUnsupportedPreviewResponse(
  overrides?: Partial<DocumentPreviewResponse>,
): DocumentPreviewResponse {
  return {
    documentId: crypto.randomUUID(),
    fileName: "fixture.docx",
    contentType:
      "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
    previewKind: "Unsupported",
    previewUrl: null,
    textContent: null,
    errorCode: "DOCUMENT_PREVIEW_UNSUPPORTED",
    message: "This document type cannot be previewed yet.",
    ...overrides,
  };
}

export function createErrorPreviewResponse(
  overrides?: Partial<DocumentPreviewResponse>,
): DocumentPreviewResponse {
  return {
    documentId: crypto.randomUUID(),
    fileName: "fixture.txt",
    contentType: "text/plain; charset=utf-8",
    previewKind: "Error",
    previewUrl: null,
    textContent: null,
    errorCode: "DOCUMENT_PREVIEW_UNAVAILABLE",
    message: "Preview is unavailable.",
    ...overrides,
  };
}
