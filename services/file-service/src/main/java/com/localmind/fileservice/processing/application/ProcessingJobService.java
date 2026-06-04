package com.localmind.fileservice.processing.application;

import com.localmind.fileservice.common.error.AppException;
import com.localmind.fileservice.common.error.ErrorCode;
import com.localmind.fileservice.files.domain.FileStatus;
import com.localmind.fileservice.metadata.application.FileMetadataRepository;
import com.localmind.fileservice.metadata.domain.FileMetadata;
import com.localmind.fileservice.processing.domain.ProcessingJob;
import java.time.Clock;
import java.time.OffsetDateTime;
import java.util.UUID;
import org.springframework.http.HttpStatus;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Service
public class ProcessingJobService {
  private final FileMetadataRepository fileMetadataRepository;
  private final ProcessingJobRepository processingJobRepository;
  private final ProcessingQueuePublisher queuePublisher;
  private final Clock clock;

  public ProcessingJobService(
      FileMetadataRepository fileMetadataRepository,
      ProcessingJobRepository processingJobRepository,
      ProcessingQueuePublisher queuePublisher,
      Clock clock) {
    this.fileMetadataRepository = fileMetadataRepository;
    this.processingJobRepository = processingJobRepository;
    this.queuePublisher = queuePublisher;
    this.clock = clock;
  }

  @Transactional
  public ProcessingJobDetails createJob(UUID fileId) {
    FileMetadata metadata = activeFileOrThrow(fileId);
    ProcessingJob job = ProcessingJob.pending(UUID.randomUUID(), fileId, OffsetDateTime.now(clock));
    ProcessingJob saved = processingJobRepository.save(job);

    queuePublisher.publish(
        new ProcessingQueueMessage(
            saved.id(), saved.fileId(), metadata.storageObjectKey(), metadata.contentType()));

    ProcessingJob published = processingJobRepository.save(saved.published(OffsetDateTime.now(clock)));
    return toDetails(published);
  }

  @Transactional(readOnly = true)
  public ProcessingJobDetails getJob(UUID fileId, UUID jobId) {
    activeFileOrThrow(fileId);
    return processingJobRepository
        .findByIdAndFileId(jobId, fileId)
        .map(this::toDetails)
        .orElseThrow(
            () ->
                new AppException(
                    ErrorCode.PROCESSING_JOB_NOT_FOUND,
                    HttpStatus.NOT_FOUND,
                    "Processing job was not found"));
  }

  private FileMetadata activeFileOrThrow(UUID fileId) {
    FileMetadata metadata =
        fileMetadataRepository
            .findById(fileId)
            .orElseThrow(
                () ->
                    new AppException(ErrorCode.FILE_NOT_FOUND, HttpStatus.NOT_FOUND, "File was not found"));

    if (metadata.status() == FileStatus.DELETED) {
      throw new AppException(ErrorCode.FILE_NOT_FOUND, HttpStatus.NOT_FOUND, "File was not found");
    }

    return metadata;
  }

  private ProcessingJobDetails toDetails(ProcessingJob job) {
    return new ProcessingJobDetails(
        job.id(),
        job.fileId(),
        job.status().name(),
        job.currentStep().name(),
        job.errorCode(),
        job.errorMessage(),
        job.createdAt(),
        job.updatedAt());
  }
}
