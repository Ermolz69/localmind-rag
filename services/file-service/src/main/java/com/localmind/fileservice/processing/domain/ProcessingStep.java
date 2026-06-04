package com.localmind.fileservice.processing.domain;

public enum ProcessingStep {
  QUEUED,
  PARSE,
  CHUNK,
  EMBED,
  INDEX,
  DONE
}
