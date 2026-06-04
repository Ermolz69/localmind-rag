package com.localmind.fileservice.storage.infrastructure;

import com.localmind.fileservice.common.config.StorageProperties;
import com.localmind.fileservice.common.error.AppException;
import com.localmind.fileservice.common.error.ErrorCode;
import com.localmind.fileservice.storage.application.DownloadedObject;
import com.localmind.fileservice.storage.application.FileStorageService;
import com.localmind.fileservice.storage.application.StoreFileCommand;
import com.localmind.fileservice.storage.application.StoredObject;
import io.minio.BucketExistsArgs;
import io.minio.GetObjectArgs;
import io.minio.MakeBucketArgs;
import io.minio.MinioClient;
import io.minio.PutObjectArgs;
import io.minio.RemoveObjectArgs;
import org.springframework.http.HttpStatus;
import org.springframework.stereotype.Service;

@Service
public class MinioFileStorageService implements FileStorageService {
  private final MinioClient minioClient;
  private final StorageProperties properties;

  public MinioFileStorageService(MinioClient minioClient, StorageProperties properties) {
    this.minioClient = minioClient;
    this.properties = properties;
  }

  @Override
  public StoredObject store(StoreFileCommand command) {
    try {
      ensureBucket();
      minioClient.putObject(
          PutObjectArgs.builder()
              .bucket(properties.bucket())
              .object(command.objectKey())
              .contentType(command.contentType())
              .stream(command.content(), command.sizeBytes(), -1)
              .build());
      return new StoredObject(properties.bucket(), command.objectKey());
    } catch (Exception exception) {
      throw new AppException(
          ErrorCode.STORAGE_WRITE_FAILED,
          HttpStatus.BAD_GATEWAY,
          "Failed to store uploaded file",
          exception);
    }
  }

  @Override
  public DownloadedObject open(String objectKey) {
    try {
      return new DownloadedObject(
          minioClient.getObject(
              GetObjectArgs.builder().bucket(properties.bucket()).object(objectKey).build()));
    } catch (Exception exception) {
      throw new AppException(
          ErrorCode.STORAGE_READ_FAILED,
          HttpStatus.BAD_GATEWAY,
          "Failed to read stored file",
          exception);
    }
  }

  @Override
  public void delete(String objectKey) {
    try {
      minioClient.removeObject(
          RemoveObjectArgs.builder().bucket(properties.bucket()).object(objectKey).build());
    } catch (Exception exception) {
      throw new AppException(
          ErrorCode.STORAGE_DELETE_FAILED,
          HttpStatus.BAD_GATEWAY,
          "Failed to delete stored file",
          exception);
    }
  }

  private void ensureBucket() throws Exception {
    boolean bucketExists =
        minioClient.bucketExists(BucketExistsArgs.builder().bucket(properties.bucket()).build());
    if (!bucketExists) {
      minioClient.makeBucket(MakeBucketArgs.builder().bucket(properties.bucket()).build());
    }
  }
}
