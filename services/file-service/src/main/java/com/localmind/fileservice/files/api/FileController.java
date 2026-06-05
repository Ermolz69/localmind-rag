package com.localmind.fileservice.files.api;

import com.localmind.fileservice.common.web.ApiResponse;
import com.localmind.fileservice.common.web.RequestIdFilter;
import com.localmind.fileservice.files.application.FileDownload;
import com.localmind.fileservice.files.application.FileService;
import com.localmind.fileservice.files.application.UploadFileCommand;
import java.io.InputStream;
import java.util.UUID;
import org.slf4j.MDC;
import org.springframework.core.io.InputStreamResource;
import org.springframework.http.ContentDisposition;
import org.springframework.http.HttpHeaders;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.DeleteMapping;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RequestPart;
import org.springframework.web.bind.annotation.RestController;
import org.springframework.web.multipart.MultipartFile;

@RestController
@RequestMapping("/api/v1/files")
public class FileController {
  private final FileService fileService;

  public FileController(FileService fileService) {
    this.fileService = fileService;
  }

  @PostMapping(consumes = MediaType.MULTIPART_FORM_DATA_VALUE)
  public ResponseEntity<ApiResponse<FileUploadResponse>> upload(
      @RequestPart("file") MultipartFile file,
      @RequestParam("ownerUserId") String ownerUserId,
      @RequestParam(value = "folderId", required = false) UUID folderId)
      throws Exception {
    try (InputStream content = file.getInputStream()) {
      FileUploadResponse response =
          FileApiMapper.toResponse(
              fileService.upload(
                  new UploadFileCommand(
                      ownerUserId, folderId, file.getOriginalFilename(), file.getContentType(), file.getSize(), content)));

      return ResponseEntity.status(201).body(ApiResponse.success(response, requestId()));
    }
  }

  @GetMapping("/{fileId}")
  public ApiResponse<FileResponse> getDetails(@PathVariable UUID fileId) {
    return ApiResponse.success(FileApiMapper.toResponse(fileService.getDetails(fileId)), requestId());
  }

  @GetMapping("/{fileId}/download")
  public ResponseEntity<InputStreamResource> download(@PathVariable UUID fileId) {
    FileDownload download = fileService.download(fileId);
    return ResponseEntity.ok()
        .contentType(MediaType.parseMediaType(download.contentType()))
        .contentLength(download.sizeBytes())
        .header(
            HttpHeaders.CONTENT_DISPOSITION,
            ContentDisposition.attachment().filename(download.fileName()).build().toString())
        .body(new InputStreamResource(download.content()));
  }

  @DeleteMapping("/{fileId}")
  public ApiResponse<Void> delete(@PathVariable UUID fileId) {
    fileService.delete(fileId);
    return ApiResponse.success(null, requestId());
  }

  private String requestId() {
    String requestId = MDC.get(RequestIdFilter.MDC_KEY);
    return requestId == null ? "" : requestId;
  }
}
