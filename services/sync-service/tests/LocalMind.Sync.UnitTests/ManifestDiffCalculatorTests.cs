namespace LocalMind.Sync.UnitTests;

using LocalMind.Sync.Application.Sync;
using LocalMind.Sync.Domain.Manifests;
using Xunit;

public sealed class ManifestDiffCalculatorTests
{
    [Fact]
    public void CalculateReturnsMissingAndDivergedItems()
    {
        ManifestItem localOnly = new("note", Guid.NewGuid(), 1, "a", DateTimeOffset.UtcNow);
        ManifestItem diverged = new("document", Guid.NewGuid(), 2, "local", DateTimeOffset.UtcNow);
        ManifestItem remoteOnly = new("bucket", Guid.NewGuid(), 1, "c", DateTimeOffset.UtcNow);

        SyncManifest local = new(Guid.NewGuid(), [localOnly, diverged], DateTimeOffset.UtcNow);
        SyncManifest remote = new(local.DeviceId, [remoteOnly, diverged with { Hash = "remote" }], DateTimeOffset.UtcNow);

        ManifestDiff diff = new ManifestDiffCalculator().Calculate(local, remote);

        Assert.Single(diff.MissingRemote);
        Assert.Single(diff.MissingLocal);
        Assert.Single(diff.Diverged);
    }
}
