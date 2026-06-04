# LocalMind Sync Service

Independent .NET 10 sync microservice for device state, sync sessions, manifests, change queues, conflict records, and background sync jobs.

## Responsibilities

- Register and track client devices.
- Create and inspect sync sessions.
- Accept push/pull/manifest requests.
- Publish sync work to RabbitMQ.
- Persist durable state in MongoDB.
- Use Redis for distributed locks, leases, cursors, and idempotency keys.

The service is remote/online infrastructure. It is not required for the desktop app's local-first workflows.

## Validation

```powershell
dotnet build LocalMind.Sync.slnx
dotnet test LocalMind.Sync.slnx
```
