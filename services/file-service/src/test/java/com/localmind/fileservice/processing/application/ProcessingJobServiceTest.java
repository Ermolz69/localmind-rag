package com.localmind.fileservice.processing.application;

import static org.assertj.core.api.Assertions.assertThat;

import com.localmind.fileservice.files.domain.FileStatus;
import com.localmind.fileservice.metadata.application.FileMetadataRepository;
import com.localmind.fileservice.metadata.domain.FileMetadata;
import com.localmind.fileservice.processing.domain.ProcessingJob;
import java.time.Clock;
import java.time.Instant;
import java.time.OffsetDateTime;
import java.time.ZoneOffset;
import java.util.HashMap;
import java.util.Map;
import java.util.Optional;
import java.util.UUID;
import org.junit.jupiter.api.Test;

class ProcessingJobServiceTest {
  private static final UUID FILE_ID = UUID.fromString("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

  @Test
  void createsAndPublishesProcessingJob() {
    InMemoryFileMetadataRepository fileRepository = new InMemoryFileMetadataRepository();
    InMemoryProcessingJobRepository jobRepository = new InMemoryProcessingJobRepository();
    RecordingQueuePublisher publisher = new RecordingQueuePublisher();
    OffsetDateTime now = OffsetDateTime.parse("2026-06-05T00:00:00Z");
    fileRepository.save(
        new FileMetadata(
            FILE_ID,
            "notes.pdf",
            "notes.pdf",
            "application/pdf",
            5,
            "localmind-files",
            "files/notes.pdf",
            "checksum",
            FileStatus.STORED,
            now,
            now));

    ProcessingJobService service =
        new ProcessingJobService(
            fileRepository,
            jobRepository,
            publisher,
            Clock.fixed(Instant.parse("2026-06-05T00:00:00Z"), ZoneOffset.UTC));

    ProcessingJobDetails details = service.createJob(FILE_ID);

    assertThat(details.fileId()).isEqualTo(FILE_ID);
    assertThat(details.status()).isEqualTo("PUBLISHED");
    assertThat(publisher.lastMessage.fileId()).isEqualTo(FILE_ID);
    assertThat(publisher.lastMessage.storageObjectKey()).isEqualTo("files/notes.pdf");
  }

  private static final class InMemoryFileMetadataRepository implements FileMetadataRepository {
    private final Map<UUID, FileMetadata> storage = new HashMap<>();

    @Override
    public FileMetadata save(FileMetadata metadata) {
      storage.put(metadata.id(), metadata);
      return metadata;
    }

    @Override
    public Optional<FileMetadata> findById(UUID id) {
      return Optional.ofNullable(storage.get(id));
    }
  }

  private static final class InMemoryProcessingJobRepository implements ProcessingJobRepository {
    private final Map<UUID, ProcessingJob> storage = new HashMap<>();

    @Override
    public ProcessingJob save(ProcessingJob job) {
      storage.put(job.id(), job);
      return job;
    }

    @Override
    public Optional<ProcessingJob> findByIdAndFileId(UUID jobId, UUID fileId) {
      return Optional.ofNullable(storage.get(jobId)).filter(job -> job.fileId().equals(fileId));
    }
  }

  private static final class RecordingQueuePublisher implements ProcessingQueuePublisher {
    private ProcessingQueueMessage lastMessage;

    @Override
    public void publish(ProcessingQueueMessage message) {
      this.lastMessage = message;
    }
  }
}
