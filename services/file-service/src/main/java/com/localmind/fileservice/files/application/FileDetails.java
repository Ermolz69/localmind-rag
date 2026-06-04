package com.localmind.fileservice.files.application;

import java.time.OffsetDateTime;
import java.util.UUID;

public record FileDetails(
    UUID id,
    String originalName,
    String sanitizedName,
    String contentType,
    long sizeBytes,
    String checksumSha256,
    String status,
    OffsetDateTime createdAt,
    OffsetDateTime updatedAt) {}
