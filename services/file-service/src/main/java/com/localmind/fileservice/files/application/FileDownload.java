package com.localmind.fileservice.files.application;

import java.io.InputStream;

public record FileDownload(
    String fileName, String contentType, long sizeBytes, InputStream content) {}
