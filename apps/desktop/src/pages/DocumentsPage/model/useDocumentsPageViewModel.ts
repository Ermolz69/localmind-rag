import { useMemo } from "react";
import {
  useDocumentList,
  useIngestionJobs,
  useProcessIngestionJob,
} from "@features/document-ingestion";
import { useDocumentUpload } from "@features/document-upload";
export function useDocumentsPageViewModel() {
  const documents = useDocumentList();
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
    ]);
  }

  return {
    ...documents,
    ...upload,
    ...process,
    ...jobs,
    error:
      documents.documentListError ??
      process.processError ??
      jobs.retryError ??
      jobs.cancelError,

    reload,
    uploadFile: upload.uploadFile,
  };
}
