export type DocumentStatus =
  | "Queued"
  | "Processing"
  | "Indexed"
  | "Failed"
  | string;

export type DocumentSummary = {
  id: string;
  bucketId: string | null;
  name: string;
  status: DocumentStatus;
  createdAt: string;
  lastError: string | null;
  tags?: Record<string, string> | null;
};

export type GetDocumentsRequest = {
  bucketId?: string | null;
  status?: string | null;
  cursor?: string | null;
  limit?: number;
};

export type UploadDocumentResponse = {
  documentId: string;
  ingestionJobId: string;
  status: string;
};

export type ProcessIngestionJobResponse = {
  jobId: string;
  status: string;
};

export type ReindexDocumentResponse = {
  documentId: string;
  ingestionJobId: string;
  status: string;
};
