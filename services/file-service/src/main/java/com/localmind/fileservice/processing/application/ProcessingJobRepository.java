package com.localmind.fileservice.processing.application;

import com.localmind.fileservice.processing.domain.ProcessingJob;
import java.util.Optional;
import java.util.UUID;

public interface ProcessingJobRepository {
  ProcessingJob save(ProcessingJob job);

  Optional<ProcessingJob> findByIdAndFileId(UUID jobId, UUID fileId);
}
