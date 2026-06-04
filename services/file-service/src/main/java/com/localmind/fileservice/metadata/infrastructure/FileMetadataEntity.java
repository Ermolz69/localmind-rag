package com.localmind.fileservice.metadata.infrastructure;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.Id;
import jakarta.persistence.Table;
import java.time.OffsetDateTime;
import java.util.UUID;

@Entity
@Table(name = "file_metadata")
public class FileMetadataEntity {
  @Id private UUID id;

  @Column(name = "owner_user_id", nullable = false, length = 128)
  private String ownerUserId;

  @Column(name = "folder_id")
  private UUID folderId;

  @Column(name = "original_name", nullable = false, length = 512)
  private String originalName;

  @Column(name = "storage_object_key", nullable = false, unique = true, length = 1024)
  private String storageObjectKey;

  @Column(name = "size_bytes", nullable = false)
  private long sizeBytes;

  @Column(name = "content_type", nullable = false)
  private String contentType;

  @Column(name = "created_at", nullable = false)
  private OffsetDateTime createdAt;

  protected FileMetadataEntity() {}

  public FileMetadataEntity(
      UUID id,
      String ownerUserId,
      UUID folderId,
      String originalName,
      String storageObjectKey,
      long sizeBytes,
      String contentType,
      OffsetDateTime createdAt) {
    this.id = id;
    this.ownerUserId = ownerUserId;
    this.folderId = folderId;
    this.originalName = originalName;
    this.storageObjectKey = storageObjectKey;
    this.sizeBytes = sizeBytes;
    this.contentType = contentType;
    this.createdAt = createdAt;
  }

  public UUID id() { return id; }
  public String ownerUserId() { return ownerUserId; }
  public UUID folderId() { return folderId; }
  public String originalName() { return originalName; }
  public String storageObjectKey() { return storageObjectKey; }
  public long sizeBytes() { return sizeBytes; }
  public String contentType() { return contentType; }
  public OffsetDateTime createdAt() { return createdAt; }
}
