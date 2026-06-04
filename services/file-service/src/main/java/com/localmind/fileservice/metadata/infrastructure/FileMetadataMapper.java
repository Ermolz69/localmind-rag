package com.localmind.fileservice.metadata.infrastructure;

import com.localmind.fileservice.metadata.domain.FileMetadata;

final class FileMetadataMapper {
  private FileMetadataMapper() {}

  static FileMetadataEntity toEntity(FileMetadata metadata) {
    return new FileMetadataEntity(
        metadata.id(),
        metadata.originalName(),
        metadata.sanitizedName(),
        metadata.contentType(),
        metadata.sizeBytes(),
        metadata.storageBucket(),
        metadata.storageObjectKey(),
        metadata.checksumSha256(),
        metadata.status(),
        metadata.createdAt(),
        metadata.updatedAt());
  }

  static FileMetadata toDomain(FileMetadataEntity entity) {
    return new FileMetadata(
        entity.id(),
        entity.originalName(),
        entity.sanitizedName(),
        entity.contentType(),
        entity.sizeBytes(),
        entity.storageBucket(),
        entity.storageObjectKey(),
        entity.checksumSha256(),
        entity.status(),
        entity.createdAt(),
        entity.updatedAt());
  }
}
