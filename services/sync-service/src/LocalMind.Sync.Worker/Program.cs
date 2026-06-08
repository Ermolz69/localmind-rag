using LocalMind.Sync.Application;
using LocalMind.Sync.Infrastructure;
using LocalMind.Sync.Worker.Consumers;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSyncApplication();
builder.Services.AddSyncInfrastructure(builder.Configuration, consumers =>
{
    consumers.AddConsumer<PushRequestedConsumer>();
});

IHost host = builder.Build();
await host.RunAsync();
