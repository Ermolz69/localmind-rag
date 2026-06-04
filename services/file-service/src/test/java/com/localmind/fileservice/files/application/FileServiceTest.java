package com.localmind.fileservice.files.application;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

import com.localmind.fileservice.common.config.FileValidationProperties;
import com.localmind.fileservice.common.error.AppException;
import com.localmind.fileservice.common.error.ErrorCode;
import com.localmind.fileservice.metadata.application.FileMetadataRepository;
import com.localmind.fileservice.metadata.domain.FileMetadata;
import com.localmind.fileservice.storage.application.DownloadedObject;
import com.localmind.fileservice.storage.application.FileStorageService;
import com.localmind.fileservice.storage.application.StoreFileCommand;
import com.localmind.fileservice.storage.application.StoredObject;
import java.io.ByteArrayInputStream;
import java.io.InputStream;
import java.io.OutputStream;
import java.time.Clock;
import java.time.Instant;
import java.time.ZoneOffset;
import java.util.HashMap;
import java.util.Map;
import java.util.Optional;
import java.util.Set;
import java.util.UUID;
import org.junit.jupiter.api.Test;

class FileServiceTest {
  private final InMemoryMetadataRepository metadataRepository = new InMemoryMetadataRepository();
  private final RecordingStorageService storageService = new RecordingStorageService();
  private final FileService fileService =
      new FileService(
          new FileValidationProperties(Set.of("pdf", "docx", "txt", "md"), 100),
          storageService,
          metadataRepository,
          Clock.fixed(Instant.parse("2026-06-05T00:00:00Z"), ZoneOffset.UTC));

  @Test
  void uploadsFileAndPersistsMetadata() {
    FileUploadResult result =
        fileService.upload(
            new UploadFileCommand(
                "Notes.PDF", "application/pdf", 5, new ByteArrayInputStream("hello".getBytes())));

    assertThat(result.sanitizedName()).isEqualTo("notes.pdf");
    assertThat(result.checksumSha256())
        .isEqualTo("2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824");
    assertThat(metadataRepository.findById(result.id())).isPresent();
    assertThat(storageService.lastCommand.objectKey()).contains(result.id().toString());
  }

  @Test
  void rejectsUnsupportedExtension() {
    assertThatThrownBy(
            () ->
                fileService.upload(
                    new UploadFileCommand(
                        "script.exe",
                        "application/octet-stream",
                        1,
                        new ByteArrayInputStream(new byte[] {1}))))
        .isInstanceOf(AppException.class)
        .extracting(exception -> ((AppException) exception).errorCode())
        .isEqualTo(ErrorCode.UNSUPPORTED_FILE_EXTENSION);
  }

  private static final class InMemoryMetadataRepository implements FileMetadataRepository {
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

  private static final class RecordingStorageService implements FileStorageService {
    private StoreFileCommand lastCommand;

    @Override
    public StoredObject store(StoreFileCommand command) {
      this.lastCommand = command;
      drain(command.content());
      return new StoredObject("localmind-files", command.objectKey());
    }

    @Override
    public DownloadedObject open(String objectKey) {
      return new DownloadedObject(new ByteArrayInputStream(new byte[0]));
    }

    @Override
    public void delete(String objectKey) {}

    private void drain(InputStream inputStream) {
      try {
        inputStream.transferTo(OutputStream.nullOutputStream());
      } catch (Exception exception) {
        throw new IllegalStateException(exception);
      }
    }
  }
}
