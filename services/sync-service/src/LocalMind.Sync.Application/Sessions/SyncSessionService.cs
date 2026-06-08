namespace LocalMind.Sync.Application.Sessions;

using LocalMind.Sync.Application.Abstractions;
using LocalMind.Sync.Application.Common;
using LocalMind.Sync.Application.Mappers;
using LocalMind.Sync.Contracts.Sessions;
using LocalMind.Sync.Domain.Sessions;

public sealed class SyncSessionService
{
    private static readonly TimeSpan SessionLease = TimeSpan.FromMinutes(15);
    private readonly IClock clock;
    private readonly IDeviceRepository devices;
    private readonly ISyncSessionRepository sessions;

    public SyncSessionService(IDeviceRepository devices, ISyncSessionRepository sessions, IClock clock)
    {
        this.devices = devices;
        this.sessions = sessions;
        this.clock = clock;
    }

    public async Task<Result<SyncSessionResponse>> CreateAsync(CreateSyncSessionRequest request, CancellationToken cancellationToken)
    {
        if (await devices.FindByIdAsync(request.DeviceId, cancellationToken) is null)
        {
            return Result<SyncSessionResponse>.Failure(ApplicationError.NotFound("DEVICE_NOT_FOUND", "Device was not found"));
        }

        SyncSession session = SyncSession.Start(request.DeviceId, SessionLease, clock.UtcNow);
        SyncSession saved = await sessions.SaveAsync(session, cancellationToken);
        return Result<SyncSessionResponse>.Success(ContractMappers.ToResponse(saved));
    }

    public async Task<Result<SyncSessionResponse>> GetAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        SyncSession? session = await sessions.FindByIdAsync(sessionId, cancellationToken);
        return session is null
            ? Result<SyncSessionResponse>.Failure(ApplicationError.NotFound("SYNC_SESSION_NOT_FOUND", "Sync session was not found"))
            : Result<SyncSessionResponse>.Success(ContractMappers.ToResponse(session));
    }
}
