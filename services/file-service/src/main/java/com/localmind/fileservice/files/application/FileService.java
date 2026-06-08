package com.localmind.fileservice.files.application;

import com.localmind.fileservice.common.config.FileValidationProperties;
import com.localmind.fileservice.common.error.AppException;
import com.localmind.fileservice.common.error.ErrorCode;
import com.localmind.fileservice.files.domain.FileExtension;
import com.localmind.fileservice.files.domain.FileNameSanitizer;
import com.localmind.fileservice.files.domain.FileStatus;
import com.localmind.fileservice.metadata.application.FileMetadataRepository;
import com.localmind.fileservice.metadata.domain.FileMetadata;
import com.localmind.fileservice.storage.application.DownloadedObject;
import com.localmind.fileservice.storage.application.FileStorageService;
import com.localmind.fileservice.storage.application.StoreFileCommand;
import com.localmind.fileservice.storage.application.StoredObject;
import com.localmind.fileservice.storage.domain.StorageObjectKey;
import java.io.InputStream;
import java.security.DigestInputStream;
import java.security.MessageDigest;
import java.time.Clock;
import java.time.OffsetDateTime;
import java.util.HexFormat;
import java.util.UUID;
import org.springframework.http.HttpStatus;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Service
public class FileService {
  private static final String DEFAULT_CONTENT_TYPE = "application/octet-stream";

  private final FileValidationProperties validationProperties;
  private final FileStorageService storageService;
  private final FileMetadataRepository metadataRepository;
  private final Clock clock;

  public FileService(
      FileValidationProperties validationProperties,
      FileStorageService storageService,
      FileMetadataRepository metadataRepository,
      Clock clock) {
    this.validationProperties = validationProperties;
    this.storageService = storageService;
    this.metadataRepository = metadataRepository;
    this.clock = clock;
  }

  @Transactional
  public FileUploadResult upload(UploadFileCommand command) {
    validateUpload(command);

    UUID fileId = UUID.randomUUID();
    String objectKey = StorageObjectKey.forFile(fileId, command.originalName(), clock);

    StoredObject storedObject =
        storageService.store(
            new StoreFileCommand(
                objectKey,
                normalizeContentType(command.contentType()),
                command.sizeBytes(),
                command.content()));

    OffsetDateTime now = OffsetDateTime.now(clock);
    FileMetadata metadata =
        FileMetadata.stored(
            fileId,
            command.ownerUserId(),
            command.folderId(),
            command.originalName(),
            storedObject.objectKey(),
            command.sizeBytes(),
            normalizeContentType(command.contentType()),
            now);

    try {
      FileMetadata saved = metadataRepository.save(metadata);
      return toUploadResult(saved);
    } catch (RuntimeException exception) {
      storageService.delete(storedObject.objectKey());
      throw exception;
    }
  }

  @Transactional(readOnly = true)
  public FileDetails getDetails(UUID fileId) {
    return toDetails(fileOrThrow(fileId));
  }

  @Transactional(readOnly = true)
  public FileDownload download(UUID fileId) {
    FileMetadata metadata = fileOrThrow(fileId);
    DownloadedObject downloadedObject = storageService.open(metadata.storageObjectKey());
    return new FileDownload(
        metadata.originalName(), metadata.contentType(), metadata.sizeBytes(), downloadedObject.content());
  }

  @Transactional
  public void delete(UUID fileId) {
    FileMetadata metadata = fileOrThrow(fileId);
    storageService.delete(metadata.storageObjectKey());
    metadataRepository.deleteById(metadata.id());
  }

  private void validateUpload(UploadFileCommand command) {
    if (command.sizeBytes() <= 0) {
      throw new AppException(ErrorCode.FILE_UPLOAD_EMPTY, HttpStatus.BAD_REQUEST, "Uploaded file is empty");
    }

    if (command.sizeBytes() > validationProperties.maxSizeBytes()) {
      throw new AppException(
          ErrorCode.FILE_SIZE_EXCEEDED,
          HttpStatus.PAYLOAD_TOO_LARGE,
          "Uploaded file exceeds configured size limit");
    }
  }

  private FileMetadata fileOrThrow(UUID fileId) {
    return metadataRepository
            .findById(fileId)
            .orElseThrow(
                () ->
                    new AppException(ErrorCode.FILE_NOT_FOUND, HttpStatus.NOT_FOUND, "File was not found"));
  }

  private String normalizeContentType(String contentType) {
    if (contentType == null || contentType.isBlank()) {
      return DEFAULT_CONTENT_TYPE;
    }
    return contentType;
  }

  private FileUploadResult toUploadResult(FileMetadata metadata) {
    return new FileUploadResult(
        metadata.id(),
        metadata.ownerUserId(),
        metadata.folderId(),
        metadata.originalName(),
        metadata.contentType(),
        metadata.sizeBytes(),
        metadata.createdAt());
  }

  private FileDetails toDetails(FileMetadata metadata) {
    return new FileDetails(
        metadata.id(),
        metadata.ownerUserId(),
        metadata.folderId(),
        metadata.originalName(),
        metadata.contentType(),
        metadata.sizeBytes(),
        metadata.createdAt());
  }
}
