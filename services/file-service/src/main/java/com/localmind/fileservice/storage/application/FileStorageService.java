package com.localmind.fileservice.storage.application;

public interface FileStorageService {
  StoredObject store(StoreFileCommand command);

  DownloadedObject open(String objectKey);

  void delete(String objectKey);
}
