package com.localmind.fileservice.storage.application;

import java.io.InputStream;

public record StoreFileCommand(
    String objectKey, String contentType, long sizeBytes, InputStream content) {}
