package com.localmind.fileservice.processing.domain;

import java.time.OffsetDateTime;
import java.util.UUID;

public record ProcessingJob(
    UUID id,
    UUID fileId,
    ProcessingJobStatus status,
    ProcessingStep currentStep,
    String errorCode,
    String errorMessage,
    OffsetDateTime createdAt,
    OffsetDateTime updatedAt) {
  public static ProcessingJob pending(UUID id, UUID fileId, OffsetDateTime now) {
    return new ProcessingJob(id, fileId, ProcessingJobStatus.PENDING, ProcessingStep.QUEUED, null, null, now, now);
  }

  public ProcessingJob published(OffsetDateTime now) {
    return new ProcessingJob(id, fileId, ProcessingJobStatus.PUBLISHED, currentStep, null, null, createdAt, now);
  }
}
