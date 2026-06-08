package com.localmind.fileservice.storage.application;

import java.io.InputStream;

public record DownloadedObject(InputStream content) {}
