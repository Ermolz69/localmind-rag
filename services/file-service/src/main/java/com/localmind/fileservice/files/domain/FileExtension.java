package com.localmind.fileservice.files.domain;

import java.util.Locale;
import java.util.Optional;

public final class FileExtension {
  private FileExtension() {}

  public static Optional<String> fromFileName(String fileName) {
    if (fileName == null || fileName.isBlank()) {
      return Optional.empty();
    }

    int dotIndex = fileName.lastIndexOf('.');
    if (dotIndex < 0 || dotIndex == fileName.length() - 1) {
      return Optional.empty();
    }

    return Optional.of(fileName.substring(dotIndex + 1).toLowerCase(Locale.ROOT));
  }
}
