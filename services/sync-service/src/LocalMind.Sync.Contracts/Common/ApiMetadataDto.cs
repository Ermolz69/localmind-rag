namespace LocalMind.Sync.Contracts.Common;

public sealed record ApiMetadataDto(DateTimeOffset Timestamp, string RequestId)
{
    public static ApiMetadataDto Now(string requestId)
    {
        return new ApiMetadataDto(DateTimeOffset.UtcNow, requestId);
    }
}
