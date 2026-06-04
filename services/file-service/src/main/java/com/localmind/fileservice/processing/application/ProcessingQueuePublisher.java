package com.localmind.fileservice.processing.application;

public interface ProcessingQueuePublisher {
  void publish(ProcessingQueueMessage message);
}
