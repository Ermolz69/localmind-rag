package com.localmind.fileservice.processing.infrastructure;

import java.util.Optional;
import java.util.UUID;
import org.springframework.data.jpa.repository.JpaRepository;

interface SpringDataProcessingJobRepository extends JpaRepository<ProcessingJobEntity, UUID> {
  Optional<ProcessingJobEntity> findByIdAndFileId(UUID id, UUID fileId);
}
