using LocalMind.ApiGateway.Api.Middlewares;
using LocalMind.ApiGateway.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add custom Infrastructure
builder.Services.AddGatewayInfrastructure(builder.Configuration);

var app = builder.Build();

// Insert the custom JWT validation middleware before YARP proxying
app.UseMiddleware<JwtValidationMiddleware>();

// Route the requests through YARP
app.MapReverseProxy();

app.Run();
