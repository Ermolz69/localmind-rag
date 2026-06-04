package com.localmind.fileservice.processing.api;

import java.time.OffsetDateTime;
import java.util.UUID;

public record ProcessingJobResponse(
    UUID id,
    UUID fileId,
    String status,
    String currentStep,
    String errorCode,
    String errorMessage,
    OffsetDateTime createdAt,
    OffsetDateTime updatedAt) {}
