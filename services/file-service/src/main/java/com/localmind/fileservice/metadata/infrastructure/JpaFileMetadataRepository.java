package com.localmind.fileservice.metadata.infrastructure;

import com.localmind.fileservice.metadata.application.FileMetadataRepository;
import com.localmind.fileservice.metadata.domain.FileMetadata;
import java.util.Optional;
import java.util.UUID;
import org.springframework.stereotype.Repository;

@Repository
public class JpaFileMetadataRepository implements FileMetadataRepository {
  private final SpringDataFileMetadataRepository repository;

  public JpaFileMetadataRepository(SpringDataFileMetadataRepository repository) {
    this.repository = repository;
  }

  @Override
  public FileMetadata save(FileMetadata metadata) {
    return FileMetadataMapper.toDomain(repository.save(FileMetadataMapper.toEntity(metadata)));
  }

  @Override
  public Optional<FileMetadata> findById(UUID id) {
    return repository.findById(id).map(FileMetadataMapper::toDomain);
  }
}
