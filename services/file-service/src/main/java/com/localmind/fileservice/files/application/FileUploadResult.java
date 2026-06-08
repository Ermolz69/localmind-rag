package com.localmind.fileservice.files.application;

import java.time.OffsetDateTime;
import java.util.UUID;

public record FileUploadResult(
    UUID id,
    String ownerUserId,
    UUID folderId,
    String originalName,
    String contentType,
    long sizeBytes,
    OffsetDateTime createdAt) {}
