using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using LocalMind.ApiGateway.Domain.Exceptions;
using LocalMind.ApiGateway.Domain.Models;
using LocalMind.ApiGateway.Domain.Ports;
using Microsoft.IdentityModel.Tokens;

namespace LocalMind.ApiGateway.Infrastructure.Auth;

public class JwksTokenValidator : ITokenValidator
{
    private readonly HttpClient _httpClient;
    private readonly string _jwksUri;
    private readonly string _issuer;
    private readonly string _audience;

    public JwksTokenValidator(HttpClient httpClient, string jwksUri, string issuer, string audience)
    {
        _httpClient = httpClient;
        _jwksUri = jwksUri;
        _issuer = issuer;
        _audience = audience;
    }

    public async Task<UserClaims?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            var jwksResponse = await _httpClient.GetStringAsync(_jwksUri, cancellationToken);
            var jwks = new JsonWebKeySet(jwksResponse);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                IssuerSigningKeys = jwks.GetSigningKeys()
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                         ?? principal.FindFirst("sub")?.Value;
            var email = principal.FindFirst(ClaimTypes.Email)?.Value;
            var role = principal.FindFirst(ClaimTypes.Role)?.Value;

            if (userId == null)
            {
                throw new GatewayException("Token does not contain a valid user identifier (sub).");
            }

            return new UserClaims(userId, email ?? string.Empty, role ?? string.Empty);
        }
        catch (SecurityTokenException ex)
        {
            throw new GatewayException("Invalid token.", ex);
        }
        catch (Exception ex)
        {
            throw new GatewayException("An error occurred during token validation.", ex);
        }
    }
}
