using KnowledgeApp.Infrastructure;
using KnowledgeApp.Worker;
using Serilog;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<Worker>();
builder.Services.AddSerilog(logger => logger.WriteTo.Console());

IHost host = builder.Build();
host.Run();
