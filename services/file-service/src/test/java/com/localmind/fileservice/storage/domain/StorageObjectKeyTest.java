package com.localmind.fileservice.storage.domain;

import static org.assertj.core.api.Assertions.assertThat;

import java.time.Clock;
import java.time.Instant;
import java.time.ZoneOffset;
import java.util.UUID;
import org.junit.jupiter.api.Test;

class StorageObjectKeyTest {
  @Test
  void createsDatePartitionedObjectKey() {
    UUID fileId = UUID.fromString("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    Clock clock = Clock.fixed(Instant.parse("2026-06-05T10:15:30Z"), ZoneOffset.UTC);

    String key = StorageObjectKey.forFile(fileId, "document.pdf", clock);

    assertThat(key).isEqualTo("files/2026/06/05/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/document.pdf");
  }
}
