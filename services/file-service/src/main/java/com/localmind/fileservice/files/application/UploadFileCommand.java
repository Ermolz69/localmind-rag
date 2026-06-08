package com.localmind.fileservice.files.application;

import java.io.InputStream;
import java.util.UUID;

public record UploadFileCommand(
    String ownerUserId, UUID folderId, String originalName, String contentType, long sizeBytes, InputStream content) {}
