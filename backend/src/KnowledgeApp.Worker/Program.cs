using KnowledgeApp.Infrastructure;
using KnowledgeApp.Observability;
using KnowledgeApp.Worker;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.AddKnowledgeAppObservability();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<Worker>();

IHost host = builder.Build();
host.Run();
