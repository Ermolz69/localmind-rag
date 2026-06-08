using LocalMind.Sync.Api.Endpoints;
using LocalMind.Sync.Api.Web;
using LocalMind.Sync.Application;
using LocalMind.Sync.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSyncApplication();
builder.Services.AddSyncInfrastructure(builder.Configuration);
builder.Services.AddProblemDetails();

WebApplication app = builder.Build();

app.UseMiddleware<RequestIdMiddleware>();
app.UseMiddleware<UnhandledExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapSyncServiceEndpoints();

app.Run();

public partial class Program;
