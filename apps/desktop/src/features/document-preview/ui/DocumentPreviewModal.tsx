import DOMPurify from "dompurify";
import { AlertCircle, FileQuestion, Loader2 } from "lucide-react";
import { useMemo } from "react";
import type { DocumentSummary } from "@entities/document";
import { MarkdownPreview } from "@features/note-editor";
import { documentsApi } from "@shared/api";
import type { OperationData } from "@shared/contracts";
import { Badge, InlineError, Modal } from "@shared/ui";

type DocumentPreviewData = OperationData<"GetDocumentPreview">;

type DocumentPreviewModalProps = {
  document: DocumentSummary | null;
  error: string | null;
  isLoading: boolean;
  open: boolean;
  preview: DocumentPreviewData | null;
  onClose: () => void;
};

export function DocumentPreviewModal({
  document,
  error,
  isLoading,
  open,
  preview,
  onClose,
}: DocumentPreviewModalProps) {
  const title = document?.name ?? "Document preview";
  const description = preview
    ? `${preview.previewKind} preview`
    : "Generating preview";

  return (
    <Modal
      title={title}
      description={description}
      open={open}
      onClose={onClose}
      panelClassName="max-h-[85vh] max-w-5xl overflow-hidden"
    >
      <PreviewHeader preview={preview} isLoading={isLoading} error={error} />
      <div className="mt-4 max-h-[70vh] min-h-[20rem] overflow-y-auto rounded-md border border-border bg-background">
        <PreviewContent preview={preview} isLoading={isLoading} error={error} />
      </div>
    </Modal>
  );
}

function PreviewHeader({
  preview,
  isLoading,
  error,
}: {
  preview: DocumentPreviewData | null;
  isLoading: boolean;
  error: string | null;
}) {
  return (
    <div className="flex flex-wrap items-center gap-2 text-sm text-muted-foreground">
      <Badge>
        {isLoading ? "Generating" : (preview?.previewKind ?? "Preview")}
      </Badge>
      {preview?.contentType ? <span>{preview.contentType}</span> : null}
      {error ? <InlineError message={error} /> : null}
    </div>
  );
}

function PreviewContent({
  preview,
  isLoading,
  error,
}: {
  preview: DocumentPreviewData | null;
  isLoading: boolean;
  error: string | null;
}) {
  if (isLoading) {
    return <LoadingPreview />;
  }

  if (error) {
    return <ErrorPreview message={error} />;
  }

  if (!preview) {
    return <ErrorPreview message="Preview data is unavailable." />;
  }

  if (preview.previewKind === "Error") {
    return (
      <ErrorPreview
        message={preview.message ?? "Preview is unavailable."}
        code={preview.errorCode}
      />
    );
  }

  if (preview.previewKind === "Unsupported") {
    return (
      <UnsupportedPreview
        message={
          preview.message ?? "This document type cannot be previewed yet."
        }
        code={preview.errorCode}
      />
    );
  }

  if (preview.previewKind === "Pdf") {
    return <PdfPreview preview={preview} />;
  }

  if (preview.previewKind === "Text") {
    return <TextPreview text={preview.textContent ?? ""} />;
  }

  if (preview.previewKind === "Markdown") {
    return <MarkdownPreview markdown={preview.textContent ?? ""} />;
  }

  if (preview.previewKind === "Html") {
    return <HtmlPreview html={preview.textContent ?? ""} />;
  }

  return (
    <UnsupportedPreview message="This preview type is not supported yet." />
  );
}

function LoadingPreview() {
  return (
    <div className="flex h-full min-h-[28rem] items-center justify-center gap-3 text-sm text-muted-foreground">
      <Loader2 className="animate-spin" size={18} aria-hidden />
      Generating preview...
    </div>
  );
}

function ErrorPreview({
  message,
  code,
}: {
  message: string;
  code?: string | null;
}) {
  return (
    <div className="flex h-full min-h-[28rem] flex-col items-center justify-center gap-3 p-8 text-center">
      <AlertCircle size={22} aria-hidden className="text-destructive" />
      <div>
        <p className="text-sm font-medium text-foreground">{message}</p>
        {code ? (
          <p className="mt-1 font-mono text-xs text-muted-foreground">{code}</p>
        ) : null}
      </div>
    </div>
  );
}

function UnsupportedPreview({
  message,
  code,
}: {
  message: string;
  code?: string | null;
}) {
  return (
    <div className="flex h-full min-h-[28rem] flex-col items-center justify-center gap-3 p-8 text-center">
      <FileQuestion size={22} aria-hidden className="text-muted-foreground" />
      <div>
        <p className="text-sm font-medium text-foreground">{message}</p>
        {code ? (
          <p className="mt-1 font-mono text-xs text-muted-foreground">{code}</p>
        ) : null}
      </div>
    </div>
  );
}

function PdfPreview({ preview }: { preview: DocumentPreviewData }) {
  if (!preview.previewUrl) {
    return <ErrorPreview message="PDF preview URL is unavailable." />;
  }

  const sourceUrl = documentsApi.getPreviewFileUrl(preview.previewUrl);

  return (
    <iframe
      className="h-full min-h-[32rem] w-full bg-background"
      title={`${preview.fileName} PDF preview`}
      src={sourceUrl}
    />
  );
}

function TextPreview({ text }: { text: string }) {
  return (
    <pre className="h-full min-h-[28rem] overflow-auto whitespace-pre-wrap break-words p-5 font-mono text-sm leading-6 text-foreground">
      {text}
    </pre>
  );
}

function HtmlPreview({ html }: { html: string }) {
  const sanitizedHtml = useMemo(
    () => DOMPurify.sanitize(html, { USE_PROFILES: { html: true } }),
    [html],
  );

  return (
    <iframe
      className="h-full min-h-[32rem] w-full bg-background"
      title="HTML document preview"
      sandbox=""
      referrerPolicy="no-referrer"
      srcDoc={sanitizedHtml}
    />
  );
}
