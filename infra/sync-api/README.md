# Sync API Infrastructure

Remote sync uses PostgreSQL and remote file storage. Local RAG does not depend on this service.

Docker contexts ignore local databases, logs, `.env` files, build outputs, and runtime/model data. Keep secrets in local environment files or CI secrets, not in Compose files.
