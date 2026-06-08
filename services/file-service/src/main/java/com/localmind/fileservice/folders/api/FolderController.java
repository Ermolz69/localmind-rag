package com.localmind.fileservice.folders.api;

import com.localmind.fileservice.folders.application.FolderService;
import com.localmind.fileservice.folders.domain.Folder;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;
import java.util.List;
import java.util.UUID;

@RestController
@RequestMapping("/api/v1/folders")
public class FolderController {
  private final FolderService folderService;

  public FolderController(FolderService folderService) {
    this.folderService = folderService;
  }

  record CreateFolderRequest(String ownerUserId, UUID parentFolderId, String name) {}

  @PostMapping
  public ResponseEntity<Folder> createFolder(@RequestBody CreateFolderRequest request) {
    var folder = folderService.createFolder(request.ownerUserId(), request.parentFolderId(), request.name());
    return ResponseEntity.ok(folder);
  }

  @GetMapping("/{folderId}")
  public ResponseEntity<Folder> getFolder(@PathVariable UUID folderId) {
    return folderService.getFolder(folderId)
        .map(ResponseEntity::ok)
        .orElse(ResponseEntity.notFound().build());
  }

  @GetMapping
  public ResponseEntity<List<Folder>> listFolders(
      @RequestParam String ownerUserId,
      @RequestParam(required = false) UUID parentFolderId) {
    return ResponseEntity.ok(folderService.listFolders(ownerUserId, parentFolderId));
  }

  @DeleteMapping("/{folderId}")
  public ResponseEntity<Void> deleteFolder(@PathVariable UUID folderId) {
    folderService.deleteFolder(folderId);
    return ResponseEntity.noContent().build();
  }
}
