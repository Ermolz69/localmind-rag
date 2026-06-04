package com.localmind.fileservice.files.application;

import java.io.InputStream;

public record UploadFileCommand(
    String originalName, String contentType, long sizeBytes, InputStream content) {}
