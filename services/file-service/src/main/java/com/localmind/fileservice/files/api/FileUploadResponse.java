package com.localmind.fileservice.files.api;

import java.time.OffsetDateTime;
import java.util.UUID;

public record FileUploadResponse(
    UUID id,
    String originalName,
    String sanitizedName,
    String contentType,
    long sizeBytes,
    String checksumSha256,
    String status,
    OffsetDateTime createdAt) {}
