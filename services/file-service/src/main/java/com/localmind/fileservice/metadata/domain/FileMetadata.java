package com.localmind.fileservice.metadata.domain;

import com.localmind.fileservice.files.domain.FileStatus;
import java.time.OffsetDateTime;
import java.util.UUID;

public record FileMetadata(
    UUID id,
    String originalName,
    String sanitizedName,
    String contentType,
    long sizeBytes,
    String storageBucket,
    String storageObjectKey,
    String checksumSha256,
    FileStatus status,
    OffsetDateTime createdAt,
    OffsetDateTime updatedAt) {
  public static FileMetadata stored(
      UUID id,
      String originalName,
      String sanitizedName,
      String contentType,
      long sizeBytes,
      String storageBucket,
      String storageObjectKey,
      String checksumSha256,
      OffsetDateTime now) {
    return new FileMetadata(
        id,
        originalName,
        sanitizedName,
        contentType,
        sizeBytes,
        storageBucket,
        storageObjectKey,
        checksumSha256,
        FileStatus.STORED,
        now,
        now);
  }

  public FileMetadata deleted(OffsetDateTime now) {
    return new FileMetadata(
        id,
        originalName,
        sanitizedName,
        contentType,
        sizeBytes,
        storageBucket,
        storageObjectKey,
        checksumSha256,
        FileStatus.DELETED,
        createdAt,
        now);
  }
}
