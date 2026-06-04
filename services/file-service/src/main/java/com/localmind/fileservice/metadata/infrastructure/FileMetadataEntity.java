package com.localmind.fileservice.metadata.infrastructure;

import com.localmind.fileservice.files.domain.FileStatus;
import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.EnumType;
import jakarta.persistence.Enumerated;
import jakarta.persistence.Id;
import jakarta.persistence.Table;
import java.time.OffsetDateTime;
import java.util.UUID;

@Entity
@Table(name = "file_metadata")
public class FileMetadataEntity {
  @Id private UUID id;

  @Column(name = "original_name", nullable = false, length = 512)
  private String originalName;

  @Column(name = "sanitized_name", nullable = false, length = 512)
  private String sanitizedName;

  @Column(name = "content_type", nullable = false)
  private String contentType;

  @Column(name = "size_bytes", nullable = false)
  private long sizeBytes;

  @Column(name = "storage_bucket", nullable = false)
  private String storageBucket;

  @Column(name = "storage_object_key", nullable = false, unique = true, length = 1024)
  private String storageObjectKey;

  @Column(name = "checksum_sha256", nullable = false, length = 64)
  private String checksumSha256;

  @Enumerated(EnumType.STRING)
  @Column(name = "status", nullable = false, length = 32)
  private FileStatus status;

  @Column(name = "created_at", nullable = false)
  private OffsetDateTime createdAt;

  @Column(name = "updated_at", nullable = false)
  private OffsetDateTime updatedAt;

  protected FileMetadataEntity() {}

  public FileMetadataEntity(
      UUID id,
      String originalName,
      String sanitizedName,
      String contentType,
      long sizeBytes,
      String storageBucket,
      String storageObjectKey,
      String checksumSha256,
      FileStatus status,
      OffsetDateTime createdAt,
      OffsetDateTime updatedAt) {
    this.id = id;
    this.originalName = originalName;
    this.sanitizedName = sanitizedName;
    this.contentType = contentType;
    this.sizeBytes = sizeBytes;
    this.storageBucket = storageBucket;
    this.storageObjectKey = storageObjectKey;
    this.checksumSha256 = checksumSha256;
    this.status = status;
    this.createdAt = createdAt;
    this.updatedAt = updatedAt;
  }

  public UUID id() {
    return id;
  }

  public String originalName() {
    return originalName;
  }

  public String sanitizedName() {
    return sanitizedName;
  }

  public String contentType() {
    return contentType;
  }

  public long sizeBytes() {
    return sizeBytes;
  }

  public String storageBucket() {
    return storageBucket;
  }

  public String storageObjectKey() {
    return storageObjectKey;
  }

  public String checksumSha256() {
    return checksumSha256;
  }

  public FileStatus status() {
    return status;
  }

  public OffsetDateTime createdAt() {
    return createdAt;
  }

  public OffsetDateTime updatedAt() {
    return updatedAt;
  }
}
