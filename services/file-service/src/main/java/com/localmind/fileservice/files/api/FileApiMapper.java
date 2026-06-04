package com.localmind.fileservice.files.api;

import com.localmind.fileservice.files.application.FileDetails;
import com.localmind.fileservice.files.application.FileUploadResult;

final class FileApiMapper {
  private FileApiMapper() {}

  static FileUploadResponse toResponse(FileUploadResult result) {
    return new FileUploadResponse(
        result.id(),
        result.originalName(),
        result.sanitizedName(),
        result.contentType(),
        result.sizeBytes(),
        result.checksumSha256(),
        result.status(),
        result.createdAt());
  }

  static FileResponse toResponse(FileDetails details) {
    return new FileResponse(
        details.id(),
        details.originalName(),
        details.sanitizedName(),
        details.contentType(),
        details.sizeBytes(),
        details.checksumSha256(),
        details.status(),
        details.createdAt(),
        details.updatedAt());
  }
}
