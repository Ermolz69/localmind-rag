package com.localmind.fileservice.metadata.infrastructure;

import com.localmind.fileservice.metadata.domain.FileMetadata;

final class FileMetadataMapper {
  private FileMetadataMapper() {}

  static FileMetadataEntity toEntity(FileMetadata metadata) {
    return new FileMetadataEntity(
        metadata.id(),
        metadata.ownerUserId(),
        metadata.folderId(),
        metadata.originalName(),
        metadata.storageObjectKey(),
        metadata.sizeBytes(),
        metadata.contentType(),
        metadata.createdAt());
  }

  static FileMetadata toDomain(FileMetadataEntity entity) {
    return new FileMetadata(
        entity.id(),
        entity.ownerUserId(),
        entity.folderId(),
        entity.originalName(),
        entity.storageObjectKey(),
        entity.sizeBytes(),
        entity.contentType(),
        entity.createdAt());
  }
}
