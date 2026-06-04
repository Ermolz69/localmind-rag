package com.localmind.fileservice.common.web;

import java.time.OffsetDateTime;

public record ApiResponse<T>(boolean success, T data, ApiError error, ApiMetadata metadata) {
  public static <T> ApiResponse<T> success(T data, String requestId) {
    return new ApiResponse<>(true, data, null, ApiMetadata.now(requestId));
  }

  public static <T> ApiResponse<T> failure(ApiError error, String requestId) {
    return new ApiResponse<>(false, null, error, ApiMetadata.now(requestId));
  }

  public record ApiMetadata(OffsetDateTime timestamp, String requestId) {
    public static ApiMetadata now(String requestId) {
      return new ApiMetadata(OffsetDateTime.now(), requestId);
    }
  }
}
