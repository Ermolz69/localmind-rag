package com.localmind.fileservice.processing.application;

import java.time.OffsetDateTime;
import java.util.UUID;

public record ProcessingJobDetails(
    UUID id,
    UUID fileId,
    String status,
    String currentStep,
    String errorCode,
    String errorMessage,
    OffsetDateTime createdAt,
    OffsetDateTime updatedAt) {}
