using System.Diagnostics;
using System.Text;
using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.Infrastructure.Services;

public sealed class TesseractOcrEngine(
    IOptions<OcrOptions> options,
    IAppPathProvider paths) : IOcrEngine
{
    public async Task<OcrTextResult> ExtractAsync(
        string imagePath,
        CancellationToken cancellationToken = default)
    {
        OcrOptions? ocrOptions = options.Value;

        if (!ocrOptions.Enabled)
        {
            return new OcrTextResult(
                Text: string.Empty,
                DetectedLanguage: null,
                DetectedScript: null,
                Confidence: null);
        }

        if (!File.Exists(imagePath))
        {
            throw new FileNotFoundException("OCR image file was not found.", imagePath);
        }

        string? languages = string.Join(
            "+",
            ocrOptions.Languages.Where(language => !string.IsNullOrWhiteSpace(language)));

        if (string.IsNullOrWhiteSpace(languages))
        {
            languages = "eng";
        }

        string? enginePath = ResolveExecutablePath(ocrOptions.EnginePath);
        string? tessDataPath = ResolveRuntimePath(ocrOptions.TessDataPath);

        string? text = await RunTesseractAsync(
            enginePath,
            tessDataPath,
            imagePath,
            languages,
            cancellationToken);

        string? cleanText = NormalizeText(text);

        if (string.IsNullOrWhiteSpace(cleanText))
        {
            return new OcrTextResult(
                Text: string.Empty,
                DetectedLanguage: null,
                DetectedScript: null,
                Confidence: null);
        }

        return new OcrTextResult(
            Text: cleanText,
            DetectedLanguage: ocrOptions.DetectLanguage ? DetectLanguage(cleanText) : null,
            DetectedScript: DetectScript(cleanText),
            Confidence: null);
    }

    private string ResolveExecutablePath(string path)
    {
        if (Path.IsPathRooted(path))
        {
            return path;
        }

        if (!ContainsDirectorySeparator(path))
        {
            return path;
        }

        return Path.GetFullPath(Path.Combine(paths.AppRootDirectory, path));
    }

    private string ResolveRuntimePath(string path)
    {
        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(paths.AppRootDirectory, path));
    }

    private static bool ContainsDirectorySeparator(string path)
    {
        return path.Contains(Path.DirectorySeparatorChar)
            || path.Contains(Path.AltDirectorySeparatorChar);
    }

    private static async Task<string> RunTesseractAsync(
        string enginePath,
        string tessDataPath,
        string imagePath,
        string languages,
        CancellationToken cancellationToken)
    {
        if (ContainsDirectorySeparator(enginePath) && !File.Exists(enginePath))
        {
            throw new FileNotFoundException("OCR engine executable was not found.", enginePath);
        }

        if (!Directory.Exists(tessDataPath))
        {
            throw new DirectoryNotFoundException($"OCR tessdata directory was not found: {tessDataPath}");
        }

        ProcessStartInfo? startInfo = new ProcessStartInfo
        {
            FileName = enginePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        startInfo.ArgumentList.Add(imagePath);
        startInfo.ArgumentList.Add("stdout");
        startInfo.ArgumentList.Add("-l");
        startInfo.ArgumentList.Add(languages);
        startInfo.ArgumentList.Add("--psm");
        startInfo.ArgumentList.Add("6");
        startInfo.ArgumentList.Add("--tessdata-dir");
        startInfo.ArgumentList.Add(tessDataPath);

        using Process? process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start OCR process.");

        Task<string>? outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        Task<string>? errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        string? output = await outputTask;
        string? error = await errorTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"OCR process failed: {error}");
        }

        return output;
    }

    private static string NormalizeText(string text)
    {
        return text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Trim();
    }

    private static string? DetectLanguage(string text)
    {
        string? lower = text.ToLowerInvariant();

        if (lower.Any(ch => ch is 'ї' or 'є' or 'і' or 'ґ'))
        {
            return "uk";
        }

        if (lower.Any(ch => ch is 'ы' or 'э' or 'ё' or 'ъ'))
        {
            return "ru";
        }

        if (lower.Any(ch => ch is >= 'a' and <= 'z'))
        {
            return "en";
        }

        return null;
    }

    private static string? DetectScript(string text)
    {
        if (text.Any(ch => ch is >= 'А' and <= 'я' or 'І' or 'і' or 'Ї' or 'ї' or 'Є' or 'є' or 'Ґ' or 'ґ'))
        {
            return "Cyrillic";
        }

        if (text.Any(ch => ch is >= 'A' and <= 'Z' or >= 'a' and <= 'z'))
        {
            return "Latin";
        }

        return null;
    }
}
