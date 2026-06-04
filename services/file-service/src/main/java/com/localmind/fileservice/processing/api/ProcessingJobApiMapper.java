package com.localmind.fileservice.processing.api;

import com.localmind.fileservice.processing.application.ProcessingJobDetails;

final class ProcessingJobApiMapper {
  private ProcessingJobApiMapper() {}

  static ProcessingJobResponse toResponse(ProcessingJobDetails details) {
    return new ProcessingJobResponse(
        details.id(),
        details.fileId(),
        details.status(),
        details.currentStep(),
        details.errorCode(),
        details.errorMessage(),
        details.createdAt(),
        details.updatedAt());
  }
}
