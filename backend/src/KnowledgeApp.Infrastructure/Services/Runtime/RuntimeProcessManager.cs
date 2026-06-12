using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KnowledgeApp.Infrastructure.Services.Runtime;

public sealed class RuntimeProcessManager
{
    private readonly ILogger<RuntimeProcessManager> logger;
    private readonly List<Process> processes = [];
    private readonly object syncRoot = new();

    public bool IsShuttingDown { get; private set; }

    public RuntimeProcessManager(
        IHostApplicationLifetime lifetime,
        ILogger<RuntimeProcessManager> logger)
    {
        this.logger = logger;
        lifetime.ApplicationStopping.Register(ShutdownAll);
    }

    public void RegisterProcess(Process process)
    {
        lock (syncRoot)
        {
            if (IsShuttingDown)
            {
                logger.LogWarning("Process registration attempted during shutdown. Process {Pid} will be killed.", process.Id);
                KillProcess(process);
                return;
            }

            processes.Add(process);
            logger.LogInformation("Started process pid={Pid}", process.Id);
        }
    }

    private void ShutdownAll()
    {
        lock (syncRoot)
        {
            IsShuttingDown = true;
        }

        logger.LogInformation("Graceful shutdown started");

        Process[] snapshot;
        lock (syncRoot)
        {
            snapshot = [.. processes];
        }

        foreach (Process process in snapshot)
        {
            try
            {
                if (process.HasExited)
                {
                    continue;
                }
            }
            catch (InvalidOperationException)
            {
                continue;
            }

            try
            {
                using CancellationTokenSource cts = new(TimeSpan.FromSeconds(3));
                logger.LogInformation("Waiting for process pid={Pid} to exit gracefully...", process.Id);
                
                process.WaitForExitAsync(cts.Token).GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("Process pid={Pid} did not exit within timeout. Killing entire process tree.", process.Id);
                KillProcess(process);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error waiting for process pid={Pid} to exit.", process.Id);
                KillProcess(process);
            }
        }

        logger.LogInformation("Graceful shutdown completed");
    }

    private void KillProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to kill process tree for pid={Pid}.", process.Id);
        }
    }
}
