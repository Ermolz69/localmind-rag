package com.localmind.fileservice.folders.infrastructure;

import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;
import java.util.List;
import java.util.UUID;

@Repository
public interface SpringDataFolderRepository extends JpaRepository<FolderEntity, UUID> {
  List<FolderEntity> findByOwnerUserIdAndParentFolderId(String ownerUserId, UUID parentFolderId);
  List<FolderEntity> findByOwnerUserIdAndParentFolderIdIsNull(String ownerUserId);
}
