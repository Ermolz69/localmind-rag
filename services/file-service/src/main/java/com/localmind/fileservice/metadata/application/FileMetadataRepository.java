package com.localmind.fileservice.metadata.application;

import com.localmind.fileservice.metadata.domain.FileMetadata;
import java.util.Optional;
import java.util.UUID;

public interface FileMetadataRepository {
  FileMetadata save(FileMetadata metadata);

  Optional<FileMetadata> findById(UUID id);

  void deleteById(UUID id);
}
