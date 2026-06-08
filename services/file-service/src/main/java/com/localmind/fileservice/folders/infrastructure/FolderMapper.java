package com.localmind.fileservice.folders.infrastructure;

import com.localmind.fileservice.folders.domain.Folder;

final class FolderMapper {
  private FolderMapper() {}

  static FolderEntity toEntity(Folder domain) {
    return new FolderEntity(
        domain.id(),
        domain.ownerUserId(),
        domain.parentFolderId(),
        domain.name(),
        domain.path(),
        domain.createdAt(),
        domain.updatedAt());
  }

  static Folder toDomain(FolderEntity entity) {
    return new Folder(
        entity.getId(),
        entity.getOwnerUserId(),
        entity.getParentFolderId(),
        entity.getName(),
        entity.getPath(),
        entity.getCreatedAt(),
        entity.getUpdatedAt());
  }
}
