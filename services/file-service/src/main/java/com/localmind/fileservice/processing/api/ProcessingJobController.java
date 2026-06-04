package com.localmind.fileservice.processing.api;

import com.localmind.fileservice.common.web.ApiResponse;
import com.localmind.fileservice.common.web.RequestIdFilter;
import com.localmind.fileservice.processing.application.ProcessingJobService;
import java.util.UUID;
import org.slf4j.MDC;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/api/v1/files/{fileId}/processing-jobs")
public class ProcessingJobController {
  private final ProcessingJobService processingJobService;

  public ProcessingJobController(ProcessingJobService processingJobService) {
    this.processingJobService = processingJobService;
  }

  @PostMapping
  public ResponseEntity<ApiResponse<ProcessingJobResponse>> create(@PathVariable UUID fileId) {
    ProcessingJobResponse response =
        ProcessingJobApiMapper.toResponse(processingJobService.createJob(fileId));
    return ResponseEntity.status(201).body(ApiResponse.success(response, requestId()));
  }

  @GetMapping("/{jobId}")
  public ApiResponse<ProcessingJobResponse> get(
      @PathVariable UUID fileId, @PathVariable UUID jobId) {
    return ApiResponse.success(
        ProcessingJobApiMapper.toResponse(processingJobService.getJob(fileId, jobId)), requestId());
  }

  private String requestId() {
    String requestId = MDC.get(RequestIdFilter.MDC_KEY);
    return requestId == null ? "" : requestId;
  }
}
