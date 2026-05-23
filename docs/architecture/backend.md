# Backend Architecture

The backend follows Clean Architecture:

- Domain has no infrastructure dependencies.
- Application owns use cases and ports.
- Infrastructure implements persistence, runtime, AI, vector, file storage, and sync adapters.
- LocalApi and SyncApi expose HTTP endpoints without business logic.
