package com.localmind.fileservice.files.api;

import java.time.OffsetDateTime;
import java.util.UUID;

public record FileUploadResponse(
    UUID id,
    String ownerUserId,
    UUID folderId,
    String originalName,
    String contentType,
    long sizeBytes,
    OffsetDateTime createdAt) {}
