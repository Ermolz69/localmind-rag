package com.localmind.fileservice.files.api;

import com.localmind.fileservice.files.application.FileDetails;
import com.localmind.fileservice.files.application.FileUploadResult;

final class FileApiMapper {
  private FileApiMapper() {}

  static FileUploadResponse toResponse(FileUploadResult result) {
    return new FileUploadResponse(
        result.id(),
        result.ownerUserId(),
        result.folderId(),
        result.originalName(),
        result.contentType(),
        result.sizeBytes(),
        result.createdAt());
  }

  static FileResponse toResponse(FileDetails details) {
    return new FileResponse(
        details.id(),
        details.ownerUserId(),
        details.folderId(),
        details.originalName(),
        details.contentType(),
        details.sizeBytes(),
        details.createdAt());
  }
}
