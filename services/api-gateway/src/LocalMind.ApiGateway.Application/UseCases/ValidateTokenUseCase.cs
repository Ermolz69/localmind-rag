using System.Threading;
using System.Threading.Tasks;
using LocalMind.ApiGateway.Domain.Exceptions;
using LocalMind.ApiGateway.Domain.Models;
using LocalMind.ApiGateway.Domain.Ports;

namespace LocalMind.ApiGateway.Application.UseCases;

public class ValidateTokenUseCase : IValidateTokenUseCase
{
    private readonly ITokenValidator _tokenValidator;

    public ValidateTokenUseCase(ITokenValidator tokenValidator)
    {
        _tokenValidator = tokenValidator;
    }

    public async Task<UserClaims?> ExecuteAsync(string authorizationHeader, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeader))
        {
            return null; // Or throw GatewayException depending on strictness
        }

        if (!authorizationHeader.StartsWith("Bearer ", System.StringComparison.OrdinalIgnoreCase))
        {
            throw new GatewayException("Invalid Authorization header format. Expected 'Bearer <token>'.");
        }

        var token = authorizationHeader.Substring("Bearer ".Length).Trim();
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new GatewayException("Token is empty.");
        }

        return await _tokenValidator.ValidateTokenAsync(token, cancellationToken);
    }
}
