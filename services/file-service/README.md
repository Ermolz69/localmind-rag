# LocalMind File Service

Spring Boot microservice for file uploads, file metadata, MinIO/S3 object storage, and processing job creation.

## Responsibilities

- Accept and validate uploaded documents.
- Store file blobs in MinIO/S3-compatible storage.
- Persist file metadata and processing job state in PostgreSQL.
- Create processing jobs without running chunking or embeddings inside the upload request.

## Local Run

```bash
gradle bootRun --args='--spring.profiles.active=local'
```

Required local dependencies:

- PostgreSQL database `localmind_files`
- MinIO bucket `localmind-files`

## API

```text
POST   /api/v1/files
GET    /api/v1/files/{fileId}
GET    /api/v1/files/{fileId}/download
DELETE /api/v1/files/{fileId}
POST   /api/v1/files/{fileId}/processing-jobs
GET    /api/v1/files/{fileId}/processing-jobs/{jobId}
```
