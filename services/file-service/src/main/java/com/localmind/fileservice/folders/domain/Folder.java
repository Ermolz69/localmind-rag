package com.localmind.fileservice.folders.domain;

import java.time.OffsetDateTime;
import java.util.UUID;

public record Folder(
    UUID id,
    String ownerUserId,
    UUID parentFolderId,
    String name,
    String path,
    OffsetDateTime createdAt,
    OffsetDateTime updatedAt) {
}
