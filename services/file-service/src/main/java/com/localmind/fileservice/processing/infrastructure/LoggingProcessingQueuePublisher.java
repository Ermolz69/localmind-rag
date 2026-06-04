package com.localmind.fileservice.processing.infrastructure;

import com.localmind.fileservice.processing.application.ProcessingQueueMessage;
import com.localmind.fileservice.processing.application.ProcessingQueuePublisher;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Component;

@Component
public class LoggingProcessingQueuePublisher implements ProcessingQueuePublisher {
  private static final Logger logger = LoggerFactory.getLogger(LoggingProcessingQueuePublisher.class);

  @Override
  public void publish(ProcessingQueueMessage message) {
    logger.info(
        "Processing job queued: jobId={}, fileId={}, contentType={}",
        message.jobId(),
        message.fileId(),
        message.contentType());
  }
}
