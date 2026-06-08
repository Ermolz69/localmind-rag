package com.localmind.fileservice.folders.application;

import java.time.Clock;
import java.time.OffsetDateTime;
import com.localmind.fileservice.folders.domain.Folder;
import org.springframework.stereotype.Service;
import java.util.List;
import java.util.Optional;
import java.util.UUID;

@Service
public class FolderService {
  private final FolderRepository folderRepository;
  private final Clock clock;

  public FolderService(FolderRepository folderRepository, Clock clock) {
    this.folderRepository = folderRepository;
    this.clock = clock;
  }

  public Folder createFolder(String ownerUserId, UUID parentFolderId, String name) {
    String parentPath = "";
    if (parentFolderId != null) {
      Folder parent = folderRepository.findById(parentFolderId)
          .orElseThrow(() -> new IllegalArgumentException("Parent folder not found"));
      parentPath = parent.path() + "/";
    }

    String path = parentPath + name;
    var now = OffsetDateTime.now(clock);

    Folder folder = new Folder(
        UUID.randomUUID(),
        ownerUserId,
        parentFolderId,
        name,
        path,
        now,
        now
    );

    return folderRepository.save(folder);
  }

  public Optional<Folder> getFolder(UUID folderId) {
    return folderRepository.findById(folderId);
  }

  public List<Folder> listFolders(String ownerUserId, UUID parentFolderId) {
    return folderRepository.findByOwnerAndParentId(ownerUserId, parentFolderId);
  }

  public void deleteFolder(UUID folderId) {
    folderRepository.deleteById(folderId);
  }
}
