package com.localmind.fileservice.common.error;

import org.springframework.http.HttpStatus;

public class AppException extends RuntimeException {
  private final ErrorCode errorCode;
  private final HttpStatus status;

  public AppException(ErrorCode errorCode, HttpStatus status, String message) {
    super(message);
    this.errorCode = errorCode;
    this.status = status;
  }

  public AppException(ErrorCode errorCode, HttpStatus status, String message, Throwable cause) {
    super(message, cause);
    this.errorCode = errorCode;
    this.status = status;
  }

  public ErrorCode errorCode() {
    return errorCode;
  }

  public HttpStatus status() {
    return status;
  }
}
