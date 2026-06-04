package com.localmind.fileservice.processing.infrastructure;

import com.localmind.fileservice.processing.domain.ProcessingJob;

final class ProcessingJobMapper {
  private ProcessingJobMapper() {}

  static ProcessingJobEntity toEntity(ProcessingJob job) {
    return new ProcessingJobEntity(
        job.id(),
        job.fileId(),
        job.status(),
        job.currentStep(),
        job.errorCode(),
        job.errorMessage(),
        job.createdAt(),
        job.updatedAt());
  }

  static ProcessingJob toDomain(ProcessingJobEntity entity) {
    return new ProcessingJob(
        entity.id(),
        entity.fileId(),
        entity.status(),
        entity.currentStep(),
        entity.errorCode(),
        entity.errorMessage(),
        entity.createdAt(),
        entity.updatedAt());
  }
}
