namespace KnowledgeApp.LocalApi.CompanionGateway;

/// <summary>Forwards an authorized gateway request to the loopback LocalApi.</summary>
public interface ICompanionForwarder
{
    Task ForwardAsync(HttpContext context, CancellationToken cancellationToken = default);
}
