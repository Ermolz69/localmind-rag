using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace KnowledgeApp.IntegrationTests.TestSupport;

public sealed class ContainerizedHttpServiceFixture : IAsyncLifetime
{
    private const ushort ContainerPort = 5678;

    private readonly IContainer container = new ContainerBuilder("hashicorp/http-echo:1.0")
        .WithPortBinding(ContainerPort, assignRandomHostPort: true)
        .WithCommand("-listen=:5678", "-text=ok")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request
            .ForPort(ContainerPort)
            .ForPath("/health")))
        .Build();

    public string BaseUrl => $"http://127.0.0.1:{container.GetMappedPublicPort(ContainerPort)}";

    public async Task InitializeAsync()
    {
        await container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await container.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class ContainerizedExternalServicesCollection : ICollectionFixture<ContainerizedHttpServiceFixture>
{
    public const string Name = "Containerized external services";
}
