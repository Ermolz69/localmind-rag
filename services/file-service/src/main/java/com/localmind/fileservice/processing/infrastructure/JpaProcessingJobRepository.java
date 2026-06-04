package com.localmind.fileservice.processing.infrastructure;

import com.localmind.fileservice.processing.application.ProcessingJobRepository;
import com.localmind.fileservice.processing.domain.ProcessingJob;
import java.util.Optional;
import java.util.UUID;
import org.springframework.stereotype.Repository;

@Repository
public class JpaProcessingJobRepository implements ProcessingJobRepository {
  private final SpringDataProcessingJobRepository repository;

  public JpaProcessingJobRepository(SpringDataProcessingJobRepository repository) {
    this.repository = repository;
  }

  @Override
  public ProcessingJob save(ProcessingJob job) {
    return ProcessingJobMapper.toDomain(repository.save(ProcessingJobMapper.toEntity(job)));
  }

  @Override
  public Optional<ProcessingJob> findByIdAndFileId(UUID jobId, UUID fileId) {
    return repository.findByIdAndFileId(jobId, fileId).map(ProcessingJobMapper::toDomain);
  }
}
