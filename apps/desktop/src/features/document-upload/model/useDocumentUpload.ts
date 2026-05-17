import { useRef, useState } from "react";
import type { UploadDocumentResponse } from "@entities/document";
import { documentsApi, getErrorMessage } from "@shared/api";

export type UploadState =
  | (UploadDocumentResponse & { fileName: string })
  | null;

type UseDocumentUploadOptions = {
  selectedBucketId: string;
  onUploaded: () => Promise<void>;
  onError: (message: string) => void;
};

export function useDocumentUpload({
  selectedBucketId,
  onUploaded,
  onError,
}: UseDocumentUploadOptions) {
  const fileInputRef = useRef<HTMLInputElement | null>(null);
  const [lastUpload, setLastUpload] = useState<UploadState>(null);
  const [isUploading, setIsUploading] = useState(false);
  const [isDragging, setIsDragging] = useState(false);

  async function uploadFile(file: File) {
    setIsUploading(true);
    try {
      const upload = await documentsApi.uploadDocument(
        file,
        selectedBucketId || null,
      );
      setLastUpload({ ...upload, fileName: file.name });
      await onUploaded();
    } catch (exception) {
      onError(getErrorMessage(exception, "Upload failed."));
    } finally {
      setIsUploading(false);
    }
  }

  return {
    fileInputRef,
    isDragging,
    isUploading,
    lastUpload,
    setIsDragging,
    setLastUpload,
    uploadFile,
  };
}
