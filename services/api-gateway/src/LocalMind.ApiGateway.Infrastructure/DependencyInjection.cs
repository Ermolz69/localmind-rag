using LocalMind.ApiGateway.Application.UseCases;
using LocalMind.ApiGateway.Domain.Ports;
using LocalMind.ApiGateway.Infrastructure.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LocalMind.ApiGateway.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddGatewayInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var authServiceUrl = configuration["AuthService:BaseUrl"] ?? "http://localhost:3001";
        var jwksUri = $"{authServiceUrl}/.well-known/jwks.json";
        var issuer = configuration["AuthService:Issuer"] ?? "localmind-auth";
        var audience = configuration["AuthService:Audience"] ?? "localmind-clients";

        services.AddHttpClient<ITokenValidator, JwksTokenValidator>(client =>
        {
            // The JwksTokenValidator uses the injected HttpClient
        })
        .ConfigurePrimaryHttpMessageHandler(() => new System.Net.Http.HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        })
        .AddTypedClient<ITokenValidator>((client, sp) => new JwksTokenValidator(client, jwksUri, issuer, audience));

        // Use Cases
        services.AddScoped<IValidateTokenUseCase, ValidateTokenUseCase>();

        // YARP Reverse Proxy
        services.AddReverseProxy()
                .LoadFromConfig(configuration.GetSection("ReverseProxy"));

        return services;
    }
}
