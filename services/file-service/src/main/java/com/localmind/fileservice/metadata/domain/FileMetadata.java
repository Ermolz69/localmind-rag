package com.localmind.fileservice.metadata.domain;

import java.time.OffsetDateTime;
import java.util.UUID;

public record FileMetadata(
    UUID id,
    String ownerUserId,
    UUID folderId,
    String originalName,
    String storageObjectKey,
    long sizeBytes,
    String contentType,
    OffsetDateTime createdAt) {
    
  public static FileMetadata stored(
      UUID id,
      String ownerUserId,
      UUID folderId,
      String originalName,
      String storageObjectKey,
      long sizeBytes,
      String contentType,
      OffsetDateTime now) {
    return new FileMetadata(
        id,
        ownerUserId,
        folderId,
        originalName,
        storageObjectKey,
        sizeBytes,
        contentType,
        now);
  }
}
