package com.localmind.fileservice.folders.infrastructure;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.Id;
import jakarta.persistence.Table;
import java.time.OffsetDateTime;
import java.util.UUID;

@Entity
@Table(name = "folders")
public class FolderEntity {
  @Id private UUID id;

  @Column(name = "owner_user_id", nullable = false, length = 128)
  private String ownerUserId;

  @Column(name = "parent_folder_id")
  private UUID parentFolderId;

  @Column(name = "name", nullable = false, length = 255)
  private String name;

  @Column(name = "path", nullable = false, length = 1024)
  private String path;

  @Column(name = "created_at", nullable = false)
  private OffsetDateTime createdAt;

  @Column(name = "updated_at", nullable = false)
  private OffsetDateTime updatedAt;

  protected FolderEntity() {}

  public FolderEntity(
      UUID id,
      String ownerUserId,
      UUID parentFolderId,
      String name,
      String path,
      OffsetDateTime createdAt,
      OffsetDateTime updatedAt) {
    this.id = id;
    this.ownerUserId = ownerUserId;
    this.parentFolderId = parentFolderId;
    this.name = name;
    this.path = path;
    this.createdAt = createdAt;
    this.updatedAt = updatedAt;
  }

  public UUID getId() { return id; }
  public String getOwnerUserId() { return ownerUserId; }
  public UUID getParentFolderId() { return parentFolderId; }
  public String getName() { return name; }
  public String getPath() { return path; }
  public OffsetDateTime getCreatedAt() { return createdAt; }
  public OffsetDateTime getUpdatedAt() { return updatedAt; }

  public void setName(String name) { this.name = name; }
  public void setPath(String path) { this.path = path; }
  public void setUpdatedAt(OffsetDateTime updatedAt) { this.updatedAt = updatedAt; }
}
