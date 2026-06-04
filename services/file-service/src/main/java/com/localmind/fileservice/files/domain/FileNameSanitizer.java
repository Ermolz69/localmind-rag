package com.localmind.fileservice.files.domain;

import java.text.Normalizer;
import java.util.Locale;

public final class FileNameSanitizer {
  private FileNameSanitizer() {}

  public static String sanitize(String originalName) {
    if (originalName == null || originalName.isBlank()) {
      return "file";
    }

    String fileName = originalName.replace('\\', '/');
    int separatorIndex = fileName.lastIndexOf('/');
    if (separatorIndex >= 0) {
      fileName = fileName.substring(separatorIndex + 1);
    }

    String normalized = Normalizer.normalize(fileName, Normalizer.Form.NFKC);
    String sanitized = normalized.replaceAll("[^A-Za-z0-9._-]", "_");
    sanitized = sanitized.replaceAll("_+", "_").replaceAll("^\\.+", "");

    if (sanitized.isBlank()) {
      return "file";
    }

    return sanitized.toLowerCase(Locale.ROOT);
  }
}
