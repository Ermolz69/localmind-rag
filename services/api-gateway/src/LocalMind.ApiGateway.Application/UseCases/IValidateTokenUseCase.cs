using System.Threading;
using System.Threading.Tasks;
using LocalMind.ApiGateway.Domain.Models;

namespace LocalMind.ApiGateway.Application.UseCases;

public interface IValidateTokenUseCase
{
    Task<UserClaims?> ExecuteAsync(string authorizationHeader, CancellationToken cancellationToken = default);
}
