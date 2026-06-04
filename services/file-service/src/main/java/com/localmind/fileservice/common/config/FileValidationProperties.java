package com.localmind.fileservice.common.config;

import java.util.Set;
import org.springframework.boot.context.properties.ConfigurationProperties;

@ConfigurationProperties(prefix = "localmind.files")
public record FileValidationProperties(Set<String> allowedExtensions, long maxSizeBytes) {
  public boolean isAllowedExtension(String extension) {
    return allowedExtensions != null && allowedExtensions.contains(extension.toLowerCase());
  }
}
