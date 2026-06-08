package com.localmind.fileservice.common.config;

import org.springframework.boot.context.properties.ConfigurationProperties;

@ConfigurationProperties(prefix = "localmind.storage")
public record StorageProperties(String bucket, String endpoint, String accessKey, String secretKey) {}
