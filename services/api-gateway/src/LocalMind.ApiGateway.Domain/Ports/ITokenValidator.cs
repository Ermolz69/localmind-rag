using System.Threading;
using System.Threading.Tasks;
using LocalMind.ApiGateway.Domain.Models;

namespace LocalMind.ApiGateway.Domain.Ports;

public interface ITokenValidator
{
    Task<UserClaims?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);
}
