import {
  useDocumentList,
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
    error:
      documents.documentListError ??
      process.processError ??
      runtime.runtimeError,
    processLastUpload,
    reload,
    uploadFile: upload.uploadFile,
  };
}
