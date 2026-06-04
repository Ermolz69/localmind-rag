package com.localmind.fileservice.folders.application;

import com.localmind.fileservice.folders.domain.Folder;
import java.util.List;
import java.util.Optional;
import java.util.UUID;

public interface FolderRepository {
  Folder save(Folder folder);
  Optional<Folder> findById(UUID id);
  List<Folder> findByOwnerAndParentId(String ownerUserId, UUID parentFolderId);
  void deleteById(UUID id);
}
