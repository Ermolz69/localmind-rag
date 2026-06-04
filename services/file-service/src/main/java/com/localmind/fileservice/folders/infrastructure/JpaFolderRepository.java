package com.localmind.fileservice.folders.infrastructure;

import com.localmind.fileservice.folders.application.FolderRepository;
import com.localmind.fileservice.folders.domain.Folder;
import org.springframework.stereotype.Component;
import java.util.List;
import java.util.Optional;
import java.util.UUID;
import java.util.stream.Collectors;

@Component
public class JpaFolderRepository implements FolderRepository {
  private final SpringDataFolderRepository repository;

  public JpaFolderRepository(SpringDataFolderRepository repository) {
    this.repository = repository;
  }

  @Override
  public Folder save(Folder folder) {
    var entity = FolderMapper.toEntity(folder);
    return FolderMapper.toDomain(repository.save(entity));
  }

  @Override
  public Optional<Folder> findById(UUID id) {
    return repository.findById(id).map(FolderMapper::toDomain);
  }

  @Override
  public List<Folder> findByOwnerAndParentId(String ownerUserId, UUID parentFolderId) {
    if (parentFolderId == null) {
        return repository.findByOwnerUserIdAndParentFolderIdIsNull(ownerUserId).stream()
            .map(FolderMapper::toDomain)
            .collect(Collectors.toList());
    }
    return repository.findByOwnerUserIdAndParentFolderId(ownerUserId, parentFolderId).stream()
        .map(FolderMapper::toDomain)
        .collect(Collectors.toList());
  }

  @Override
  public void deleteById(UUID id) {
    repository.deleteById(id);
  }
}
