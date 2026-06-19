using System.Diagnostics;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.Infrastructure.Services.DocumentPreview;

public sealed class LibreOfficeDocumentPreviewConverterProcess(
    IOptions<DocumentPreviewOptions> options) : IDocumentPreviewConverterProcess
{
    private static readonly string[] WindowsLibreOfficePaths =
    [
        @"C:\Program Files\LibreOffice\program\soffice.exe",
        @"C:\Program Files (x86)\LibreOffice\program\soffice.exe",
    ];

    private readonly DocumentPreviewOptions previewOptions = options.Value;

    public async Task<Result<DocumentPreviewProcessResult>> ConvertToPdfAsync(
        string sourcePath,
        string outputDirectory,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        string? executable = ResolveLibreOfficeExecutable();
        if (executable is null)
        {
            return Result<DocumentPreviewProcessResult>.Failure(ApplicationErrors.ExternalDependency(
                ErrorCodes.Documents.PreviewConverterUnavailable,
                ErrorMessages.Documents.PreviewConverterUnavailable));
        }

        string profileDirectory = Path.Combine(outputDirectory, "profile");
        string convertedDirectory = Path.Combine(outputDirectory, "converted");

        Directory.CreateDirectory(profileDirectory);
        Directory.CreateDirectory(convertedDirectory);

        using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);

        using Process process = new()
        {
            StartInfo = CreateStartInfo(
                executable,
                sourcePath,
                convertedDirectory,
                profileDirectory),
        };

        try
        {
            process.Start();

            Task<string> stderrTask = process.StandardError.ReadToEndAsync(timeoutCts.Token);
            Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync(timeoutCts.Token);

            await process.WaitForExitAsync(timeoutCts.Token);
            await Task.WhenAll(stderrTask, stdoutTask);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            TryKill(process);

            return Result<DocumentPreviewProcessResult>.Failure(ApplicationErrors.ExternalDependency(
                ErrorCodes.Documents.PreviewConversionTimeout,
                ErrorMessages.Documents.PreviewConversionTimeout));
        }

        if (process.ExitCode != 0)
        {
            return Result<DocumentPreviewProcessResult>.Failure(ApplicationErrors.Unexpected(
                ErrorCodes.Documents.PreviewConversionFailed,
                ErrorMessages.Documents.PreviewConversionFailed));
        }

        string pdfPath = Path.Combine(
            convertedDirectory,
            Path.GetFileNameWithoutExtension(sourcePath) + ".pdf");

        if (!File.Exists(pdfPath))
        {
            pdfPath = Directory
                .EnumerateFiles(convertedDirectory, "*.pdf", SearchOption.TopDirectoryOnly)
                .FirstOrDefault() ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(pdfPath))
        {
            return Result<DocumentPreviewProcessResult>.Failure(ApplicationErrors.Unexpected(
                ErrorCodes.Documents.PreviewConversionFailed,
                ErrorMessages.Documents.PreviewConversionFailed));
        }

        return Result<DocumentPreviewProcessResult>.Success(new DocumentPreviewProcessResult(pdfPath));
    }

    private string? ResolveLibreOfficeExecutable()
    {
        if (!string.IsNullOrWhiteSpace(previewOptions.LibreOfficePath))
        {
            return File.Exists(previewOptions.LibreOfficePath)
                ? previewOptions.LibreOfficePath
                : null;
        }

        string? pathExecutable = FindOnPath("soffice") ?? FindOnPath("soffice.exe");
        if (pathExecutable is not null)
        {
            return pathExecutable;
        }

        return WindowsLibreOfficePaths.FirstOrDefault(File.Exists);
    }

    private static string? FindOnPath(string executableName)
    {
        string? path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        foreach (string directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            string candidate = Path.Combine(directory.Trim(), executableName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static ProcessStartInfo CreateStartInfo(
        string executable,
        string sourcePath,
        string outputDirectory,
        string profileDirectory)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = executable,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        startInfo.ArgumentList.Add("--headless");
        startInfo.ArgumentList.Add("--nologo");
        startInfo.ArgumentList.Add("--nodefault");
        startInfo.ArgumentList.Add("--nofirststartwizard");
        startInfo.ArgumentList.Add("--nolockcheck");
        startInfo.ArgumentList.Add($"--env:UserInstallation={new Uri(profileDirectory).AbsoluteUri}");
        startInfo.ArgumentList.Add("--convert-to");
        startInfo.ArgumentList.Add("pdf");
        startInfo.ArgumentList.Add("--outdir");
        startInfo.ArgumentList.Add(outputDirectory);
        startInfo.ArgumentList.Add(sourcePath);

        return startInfo;
    }

    private static void TryKill(Process process)
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
    }
}
