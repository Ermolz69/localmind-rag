import { useMemo } from "react";
import {
  filterDocumentsByLifecycleStatus,
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

  const shouldLoadJobs = useMemo(() => {
    return upload.lastUpload !== null || documents.documents.length > 0;
  }, [upload.lastUpload, documents.documents]);

  const jobs = useIngestionJobs(shouldLoadJobs, () => {
    void documents.reloadDocuments();
  });

  const filteredDocuments = useMemo(
    () =>
      filterDocumentsByLifecycleStatus(
        documents.documents,
        jobs.jobsByDocumentId,
        documents.selectedStatus,
      ),
    [documents.documents, documents.selectedStatus, jobs.jobsByDocumentId],
  );

  async function reload() {
    await Promise.all([documents.reloadDocuments(), documents.loadBuckets()]);
  }

  return {
    ...documents,
    ...upload,
    ...process,
    ...jobs,
    filteredDocuments,
    error:
      documents.documentListError ??
      process.processError ??
      jobs.retryError ??
      jobs.cancelError,

    reload,
    uploadFile: upload.uploadFile,
  };
}
