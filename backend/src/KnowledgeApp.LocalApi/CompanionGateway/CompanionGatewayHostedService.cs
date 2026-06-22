using System.Net;
using KnowledgeApp.Application.Companion;
using KnowledgeApp.Application.Settings;
using KnowledgeApp.Contracts.Settings;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.LocalApi.CompanionGateway;

/// <summary>
/// Runs the LAN companion gateway only while Companion Mode is enabled. It owns a
/// second Kestrel listener (separate from the loopback LocalApi) that serves the
/// SPA and reverse-proxies the allowlisted API to loopback. The listener is
/// started/stopped in response to the settings-change signal.
/// </summary>
public sealed class CompanionGatewayHostedService(
    IServiceProvider services,
    ISettingsChangeSignal settingsChangeSignal,
    ICompanionPairingService pairing,
    IServer server,
    IConfiguration configuration,
    IOptions<CompanionGatewayOptions> options,
    ILogger<CompanionGatewayHostedService> logger) : IHostedService
{
    private readonly CompanionGatewayOptions options = options.Value;
    private readonly SemaphoreSlim reconcileLock = new(1, 1);

    private CancellationTokenSource? loopCts;
    private Task? loopTask;
    private WebApplication? gateway;
    private HttpClient? gatewayClient;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        loopCts = new CancellationTokenSource();
        loopTask = Task.Run(() => RunAsync(loopCts.Token), CancellationToken.None);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        loopCts?.Cancel();

        if (loopTask is not null)
        {
            try
            {
                await loopTask;
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown.
            }
        }

        await StopGatewayAsync();
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        await ReconcileAsync(cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await settingsChangeSignal.ReadAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await ReconcileAsync(cancellationToken);
        }
    }

    private async Task ReconcileAsync(CancellationToken cancellationToken)
    {
        await reconcileLock.WaitAsync(cancellationToken);
        try
        {
            bool enabled = await IsEnabledAsync(cancellationToken);

            if (enabled && gateway is null)
            {
                await StartGatewayAsync();
            }
            else if (!enabled && gateway is not null)
            {
                await StopGatewayAsync();
            }
        }
        catch (OperationCanceledException)
        {
            // Shutdown.
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Companion gateway reconcile failed.");
        }
        finally
        {
            reconcileLock.Release();
        }
    }

    private async Task<bool> IsEnabledAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = services.CreateScope();
        ISettingsService settings = scope.ServiceProvider.GetRequiredService<ISettingsService>();
        AppSettingsDto dto = await settings.GetAsync(cancellationToken);
        return dto.CompanionMode?.Enabled ?? false;
    }

    private async Task StartGatewayAsync()
    {
        string loopbackBaseUrl = ResolveLoopbackBaseUrl();
        IFileProvider? spaFiles = ResolveSpaFiles();

        gatewayClient = new HttpClient(new SocketsHttpHandler
        {
            UseProxy = false,
            AllowAutoRedirect = false,
            UseCookies = false,
            AutomaticDecompression = DecompressionMethods.None,
        });

        string? token = configuration["LocalApiSecurity:Token"]
            ?? Environment.GetEnvironmentVariable("LOCALMIND_LOCAL_API_TOKEN");
        var forwarder = new HttpCompanionForwarder(gatewayClient, loopbackBaseUrl, token);

        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = [],
        });
        builder.WebHost.UseUrls($"http://0.0.0.0:{options.Port}");
        builder.Logging.ClearProviders();
        builder.Services.AddRouting();

        WebApplication app = builder.Build();
        CompanionGatewayPipeline.Configure(app, pairing, forwarder, spaFiles);

        await app.StartAsync();
        gateway = app;

        logger.LogInformation(
            "Companion gateway listening on port {Port}, proxying to {Loopback} (spa: {HasSpa}).",
            options.Port,
            loopbackBaseUrl,
            spaFiles is not null);
    }

    private async Task StopGatewayAsync()
    {
        WebApplication? app = gateway;
        gateway = null;

        if (app is not null)
        {
            try
            {
                await app.StopAsync();
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Companion gateway stop failed.");
            }

            await app.DisposeAsync();
            logger.LogInformation("Companion gateway stopped.");
        }

        gatewayClient?.Dispose();
        gatewayClient = null;
    }

    private string ResolveLoopbackBaseUrl()
    {
        IServerAddressesFeature? feature = server.Features.Get<IServerAddressesFeature>();
        string? address = feature?.Addresses.FirstOrDefault(
            a => a.Contains("127.0.0.1", StringComparison.Ordinal)
                || a.Contains("localhost", StringComparison.OrdinalIgnoreCase));

        if (address is not null && Uri.TryCreate(address, UriKind.Absolute, out Uri? uri))
        {
            return $"http://127.0.0.1:{uri.Port}";
        }

        string? envPort = Environment.GetEnvironmentVariable("LOCALMIND_LOCAL_API_PORT");
        int port = int.TryParse(envPort, out int parsed) ? parsed : 49321;
        return $"http://127.0.0.1:{port}";
    }

    private IFileProvider? ResolveSpaFiles()
    {
        string? path = options.StaticPath;

        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            logger.LogWarning(
                "Companion gateway static path not found ({Path}); serving API only.",
                path ?? "(none)");
            return null;
        }

        return new PhysicalFileProvider(Path.GetFullPath(path));
    }
}
