package com.localmind.fileservice.processing.infrastructure;

import com.localmind.fileservice.processing.domain.ProcessingJobStatus;
import com.localmind.fileservice.processing.domain.ProcessingStep;
import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.EnumType;
import jakarta.persistence.Enumerated;
import jakarta.persistence.Id;
import jakarta.persistence.Table;
import java.time.OffsetDateTime;
import java.util.UUID;

@Entity
@Table(name = "processing_jobs")
public class ProcessingJobEntity {
  @Id private UUID id;

  @Column(name = "file_id", nullable = false)
  private UUID fileId;

  @Enumerated(EnumType.STRING)
  @Column(name = "status", nullable = false, length = 32)
  private ProcessingJobStatus status;

  @Enumerated(EnumType.STRING)
  @Column(name = "current_step", nullable = false, length = 64)
  private ProcessingStep currentStep;

  @Column(name = "error_code", length = 128)
  private String errorCode;

  @Column(name = "error_message", length = 1024)
  private String errorMessage;

  @Column(name = "created_at", nullable = false)
  private OffsetDateTime createdAt;

  @Column(name = "updated_at", nullable = false)
  private OffsetDateTime updatedAt;

  protected ProcessingJobEntity() {}

  public ProcessingJobEntity(
      UUID id,
      UUID fileId,
      ProcessingJobStatus status,
      ProcessingStep currentStep,
      String errorCode,
      String errorMessage,
      OffsetDateTime createdAt,
      OffsetDateTime updatedAt) {
    this.id = id;
    this.fileId = fileId;
    this.status = status;
    this.currentStep = currentStep;
    this.errorCode = errorCode;
    this.errorMessage = errorMessage;
    this.createdAt = createdAt;
    this.updatedAt = updatedAt;
  }

  public UUID id() {
    return id;
  }

  public UUID fileId() {
    return fileId;
  }

  public ProcessingJobStatus status() {
    return status;
  }

  public ProcessingStep currentStep() {
    return currentStep;
  }

  public String errorCode() {
    return errorCode;
  }

  public String errorMessage() {
    return errorMessage;
  }

  public OffsetDateTime createdAt() {
    return createdAt;
  }

  public OffsetDateTime updatedAt() {
    return updatedAt;
  }
}
