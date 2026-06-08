package com.localmind.fileservice.metadata.infrastructure;

import java.util.UUID;
import org.springframework.data.jpa.repository.JpaRepository;

interface SpringDataFileMetadataRepository extends JpaRepository<FileMetadataEntity, UUID> {}
