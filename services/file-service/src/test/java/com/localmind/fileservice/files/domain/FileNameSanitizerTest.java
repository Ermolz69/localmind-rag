package com.localmind.fileservice.files.domain;

import static org.assertj.core.api.Assertions.assertThat;

import org.junit.jupiter.api.Test;

class FileNameSanitizerTest {
  @Test
  void removesPathSegmentsAndUnsafeCharacters() {
    String sanitized = FileNameSanitizer.sanitize("C:\\temp\\My Report (final).PDF");

    assertThat(sanitized).isEqualTo("my_report_final_.pdf");
  }

  @Test
  void fallsBackWhenNameIsBlank() {
    assertThat(FileNameSanitizer.sanitize("   ")).isEqualTo("file");
  }
}
