package com.localmind.fileservice.storage.domain;

import java.time.Clock;
import java.time.LocalDate;
import java.util.UUID;

public final class StorageObjectKey {
  private StorageObjectKey() {}

  public static String forFile(UUID fileId, String sanitizedName, Clock clock) {
    LocalDate today = LocalDate.now(clock);
    return "files/%04d/%02d/%02d/%s/%s"
        .formatted(today.getYear(), today.getMonthValue(), today.getDayOfMonth(), fileId, sanitizedName);
  }
}
