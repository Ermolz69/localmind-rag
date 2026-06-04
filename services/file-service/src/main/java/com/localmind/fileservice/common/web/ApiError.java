package com.localmind.fileservice.common.web;

import com.localmind.fileservice.common.error.ErrorCode;
import java.util.Map;

public record ApiError(ErrorCode code, String message, Map<String, String> details) {
  public static ApiError of(ErrorCode code, String message) {
    return new ApiError(code, message, Map.of());
  }
}
