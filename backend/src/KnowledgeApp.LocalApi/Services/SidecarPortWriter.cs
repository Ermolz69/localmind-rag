using KnowledgeApp.Application.Abstractions;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace KnowledgeApp.LocalApi.Services;

public class SidecarPortWriter : IHostedService
{
    private readonly IServer _server;
    private readonly IAppPathProvider _appPathProvider;
    private readonly ILogger<SidecarPortWriter> _logger;

    public SidecarPortWriter(
        IServer server,
        IAppPathProvider appPathProvider,
        ILogger<SidecarPortWriter> logger)
    {
        _server = server;
        _appPathProvider = appPathProvider;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var addresses = _server.Features.Get<IServerAddressesFeature>();
        if (addresses != null)
        {
            var address = addresses.Addresses.FirstOrDefault();
            if (address != null)
            {
                if (Uri.TryCreate(address, UriKind.Absolute, out var uri))
                {
                    var port = uri.Port;
                    var portFilePath = Path.Combine(_appPathProvider.DataDirectory, "sidecar-port.txt");
                    
                    try
                    {
                        File.WriteAllText(portFilePath, port.ToString());
                        _logger.LogInformation("Written sidecar port {Port} to {PortFilePath}", port, portFilePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to write sidecar port to {PortFilePath}", portFilePath);
                    }
                }
            }
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
