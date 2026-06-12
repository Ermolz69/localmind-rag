import { useRef, useState } from "react";
import type { UploadDocumentResponse } from "@entities/document";
import { documentsApi, getFieldError } from "@shared/api";
import { useApiMutation } from "@shared/lib/hooks";

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
  const [isDragging, setIsDragging] = useState(false);

  const uploadMutation = useApiMutation(
    (file: File) =>
      documentsApi.uploadDocument(file, selectedBucketId || undefined),
    { fallbackError: "Upload failed." },
  );

  async function uploadFile(file: File) {
    const upload = await uploadMutation.mutate(file);
    if (upload) {
      setLastUpload({ ...upload, fileName: file.name });
      await onUploaded();
    } else if (uploadMutation.rawError) {
      onError(
        getFieldError(uploadMutation.rawError, "file") ??
          getFieldError(uploadMutation.rawError, "fileName") ??
          uploadMutation.error ??
          "Upload failed.",
      );
    }
  }

  return {
    fileInputRef,
    isDragging,
    isUploading: uploadMutation.isPending,
    lastUpload,
    setIsDragging,
    setLastUpload,
    uploadFile,
  };
}
