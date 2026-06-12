import type { OperationData, OperationQuery, Schema } from "@shared/contracts";

export type DocumentSummary = Schema<"DocumentDto">;
export type DocumentStatus = DocumentSummary["status"];
export type GetDocumentsRequest = OperationQuery<"ListDocuments">;
export type UploadDocumentResponse = OperationData<"UploadDocument">;
export type ProcessIngestionJobResponse = OperationData<"ProcessIngestionJob">;
export type ReindexDocumentResponse = OperationData<"ReindexDocument">;
