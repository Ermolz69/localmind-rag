package com.localmind.fileservice.processing.application;

import java.util.UUID;

public record ProcessingQueueMessage(UUID jobId, UUID fileId, String storageObjectKey, String contentType) {}
