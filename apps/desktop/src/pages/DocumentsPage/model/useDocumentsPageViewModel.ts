import { useMemo } from "react";
import {
  useDocumentList,
  useIngestionJobs,
  useProcessIngestionJob,
} from "@features/document-ingestion";
import { useDocumentUpload } from "@features/document-upload";
import { useRuntimeStatus } from "@shared/model";

export function useDocumentsPageViewModel() {
  const documents = useDocumentList();
  const runtime = useRuntimeStatus();
  const upload = useDocumentUpload({
    onError: documents.setDocumentListError,
    onUploaded: async () => {
      await documents.reloadDocuments();
    },
    selectedBucketId: documents.selectedBucketId,
  });
  const process = useProcessIngestionJob({
    onProcessed: async () => {
      await documents.reloadDocuments();
    },
  });

  const shouldPollJobs = useMemo(() => {
    return (
      upload.lastUpload !== null ||
      documents.documents.some(
        (d) => d.status === "Queued" || d.status === "Processing",
      )
    );
  }, [upload.lastUpload, documents.documents]);

  const jobs = useIngestionJobs(shouldPollJobs, () => {
    void documents.reloadDocuments();
  });

  async function reload() {
    await Promise.all([
      documents.reloadDocuments(),
      documents.loadBuckets(),
      runtime.loadRuntimeStatus(),
    ]);
  }

  async function processLastUpload() {
    if (!upload.lastUpload) {
      return;
    }

    await process.processUploadedDocument(
      upload.lastUpload.documentId,
      upload.lastUpload.ingestionJobId,
      () => upload.setLastUpload(null),
    );
  }

  return {
    ...documents,
    ...runtime,
    ...upload,
    ...process,
    ...jobs,
    error:
      documents.documentListError ??
      process.processError ??
      runtime.runtimeError ??
      jobs.retryError ??
      jobs.cancelError,
    processLastUpload,
    reload,
    uploadFile: upload.uploadFile,
  };
}
