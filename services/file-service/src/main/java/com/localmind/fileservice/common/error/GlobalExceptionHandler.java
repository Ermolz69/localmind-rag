package com.localmind.fileservice.common.error;

import com.localmind.fileservice.common.web.ApiError;
import com.localmind.fileservice.common.web.ApiResponse;
import com.localmind.fileservice.common.web.RequestIdFilter;
import jakarta.servlet.http.HttpServletRequest;
import java.util.Map;
import java.util.stream.Collectors;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.slf4j.MDC;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.MethodArgumentNotValidException;
import org.springframework.web.bind.annotation.ExceptionHandler;
import org.springframework.web.bind.annotation.RestControllerAdvice;
import org.springframework.web.multipart.MaxUploadSizeExceededException;

@RestControllerAdvice
public class GlobalExceptionHandler {
  private static final Logger logger = LoggerFactory.getLogger(GlobalExceptionHandler.class);

  @ExceptionHandler(AppException.class)
  public ResponseEntity<ApiResponse<Void>> handleAppException(AppException exception) {
    ApiError error = ApiError.of(exception.errorCode(), exception.getMessage());
    return ResponseEntity.status(exception.status()).body(ApiResponse.failure(error, requestId()));
  }

  @ExceptionHandler(MethodArgumentNotValidException.class)
  public ResponseEntity<ApiResponse<Void>> handleValidation(MethodArgumentNotValidException exception) {
    Map<String, String> details =
        exception.getBindingResult().getFieldErrors().stream()
            .collect(
                Collectors.toMap(
                    fieldError -> fieldError.getField(),
                    fieldError -> fieldError.getDefaultMessage() == null ? "Invalid value" : fieldError.getDefaultMessage(),
                    (first, second) -> first));

    ApiError error = new ApiError(ErrorCode.VALIDATION_FAILED, "Request validation failed", details);
    return ResponseEntity.badRequest().body(ApiResponse.failure(error, requestId()));
  }

  @ExceptionHandler(MaxUploadSizeExceededException.class)
  public ResponseEntity<ApiResponse<Void>> handleMaxUploadSize(MaxUploadSizeExceededException exception) {
    ApiError error = ApiError.of(ErrorCode.FILE_SIZE_EXCEEDED, "Uploaded file exceeds configured size limit");
    return ResponseEntity.status(HttpStatus.PAYLOAD_TOO_LARGE).body(ApiResponse.failure(error, requestId()));
  }

  @ExceptionHandler(Exception.class)
  public ResponseEntity<ApiResponse<Void>> handleUnexpected(Exception exception, HttpServletRequest request) {
    logger.error("Unhandled error while processing {} {}", request.getMethod(), request.getRequestURI(), exception);
    ApiError error = ApiError.of(ErrorCode.INTERNAL_SERVER_ERROR, "Unexpected file service error");
    return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR).body(ApiResponse.failure(error, requestId()));
  }

  private String requestId() {
    String requestId = MDC.get(RequestIdFilter.MDC_KEY);
    return requestId == null ? "" : requestId;
  }
}
