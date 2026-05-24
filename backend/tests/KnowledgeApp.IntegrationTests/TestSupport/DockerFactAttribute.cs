using System.Diagnostics;

namespace KnowledgeApp.IntegrationTests.TestSupport;

public sealed class DockerFactAttribute : FactAttribute
{
    public DockerFactAttribute()
    {
        if (!IsContinuousIntegration() && !IsDockerAvailable())
        {
            Skip = "Docker is not available. Start Docker to run container-backed integration tests.";
        }
    }

    private static bool IsContinuousIntegration()
    {
        return string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDockerAvailable()
    {
        try
        {
            using Process process = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    ArgumentList = { "version", "--format", "{{.Server.Version}}" },
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                },
            };

            process.Start();
            return process.WaitForExit(3_000) && process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
