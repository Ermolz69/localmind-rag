using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Exceptions;
using KnowledgeApp.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.Infrastructure.Services;

public sealed class AiRuntimeProviderRegistry(
    IEnumerable<IAiRuntimeProvider> providers,
    IOptions<AiOptions> options) : IAiRuntimeProviderRegistry
{
    private readonly IReadOnlyCollection<IAiRuntimeProvider> providers = providers.ToArray();
    private readonly AiOptions options = options.Value;

    public IReadOnlyCollection<IAiRuntimeProvider> Providers => providers;

    public IAiRuntimeProvider GetSelectedProvider()
    {
        string configuredProvider = string.IsNullOrWhiteSpace(options.Provider)
            ? "LlamaCpp"
            : options.Provider;

        IAiRuntimeProvider? provider = providers.FirstOrDefault(provider =>
            string.Equals(provider.ProviderId, configuredProvider, StringComparison.OrdinalIgnoreCase)
            || string.Equals(provider.ProviderName, configuredProvider, StringComparison.OrdinalIgnoreCase)
            || string.Equals(provider.ProviderId.Replace("-", string.Empty, StringComparison.Ordinal), configuredProvider, StringComparison.OrdinalIgnoreCase));

        return provider ?? throw new ExternalDependencyAppException(
            ErrorCodes.Runtime.AiProviderNotFound,
            ErrorMessages.Runtime.AiProviderNotFound);
    }
}
