using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Application.Ingestion.WatchedFolders;
using KnowledgeApp.Contracts.Settings;
using KnowledgeApp.Domain.Enums;

namespace KnowledgeApp.Application.Settings;

public sealed class SettingsValidator(IWatchedFolderPathValidator watchedFolderPathValidator)
{
    private const int MinDebounceMilliseconds = 250;
    private const int MaxDebounceMilliseconds = 60000;
    private const int MinLogRetainedDays = 1;
    private const int MaxLogRetainedDays = 365;
    private const string MarkDeletedPolicy = "MarkDeleted";
    private static readonly HashSet<string> SupportedLogLevels = new(StringComparer.OrdinalIgnoreCase)
    {
        "Trace",
        "Debug",
        "Information",
        "Warning",
        "Error",
        "Critical",
        "None",
    };

    public Result Validate(AppSettingsDto request)
    {
        Dictionary<string, string[]> errors = new Dictionary<string, string[]>();

        if (!Enum.TryParse<AppTheme>(request.Appearance.Theme, ignoreCase: true, out _))
        {
            errors["appearance.theme"] = [ErrorMessages.Settings.ThemeInvalid];
        }

        if (!Enum.TryParse<AiProviderType>(request.Ai.Provider, ignoreCase: true, out _))
        {
            errors["ai.provider"] = [ErrorMessages.Settings.AiProviderInvalid];
        }

        AddRequired(errors, "ai.chatModel", request.Ai.ChatModel);
        AddRequired(errors, "ai.embeddingModel", request.Ai.EmbeddingModel);
        AddRequired(errors, "ai.runtimePath", request.Ai.RuntimePath);
        AddRequired(errors, "ai.modelsPath", request.Ai.ModelsPath);

        AddRequired(errors, "runtimePaths.dataPath", request.RuntimePaths.DataPath);
        AddRequired(errors, "runtimePaths.databasePath", request.RuntimePaths.DatabasePath);
        AddRequired(errors, "runtimePaths.filesPath", request.RuntimePaths.FilesPath);
        AddRequired(errors, "runtimePaths.indexPath", request.RuntimePaths.IndexPath);
        AddRequired(errors, "runtimePaths.logsPath", request.RuntimePaths.LogsPath);

        ValidateDiagnostics(errors, request.Diagnostics);
        ValidateWatchedFolders(errors, request);

        if (errors.Count > 0)
        {
            return Result.Failure(ApplicationErrors.Validation(
                ErrorCodes.Settings.ValidationFailed,
                ErrorMessages.Settings.ValidationFailed,
                errors));
        }

        return Result.Success();
    }

    private static void ValidateDiagnostics(
        Dictionary<string, string[]> errors,
        DiagnosticsSettingsDto? diagnostics)
    {
        if (diagnostics is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(diagnostics.MinimumLogLevel) ||
            !SupportedLogLevels.Contains(diagnostics.MinimumLogLevel))
        {
            errors["diagnostics.minimumLogLevel"] = ["Unsupported diagnostics log level."];
        }

        if (diagnostics.LogRetainedDays is < MinLogRetainedDays or > MaxLogRetainedDays)
        {
            errors["diagnostics.logRetainedDays"] =
            [
                $"Log retention must be between {MinLogRetainedDays} and {MaxLogRetainedDays} days."
            ];
        }
    }

    private void ValidateWatchedFolders(
        Dictionary<string, string[]> errors,
        AppSettingsDto request)
    {
        WatchedFoldersSettingsDto? watchedFolders = request.WatchedFolders;

        if (watchedFolders is null)
        {
            return;
        }

        if (watchedFolders.DebounceMilliseconds is < MinDebounceMilliseconds or > MaxDebounceMilliseconds)
        {
            errors["watchedFolders.debounceMilliseconds"] =
            [
                $"Debounce must be between {MinDebounceMilliseconds} and {MaxDebounceMilliseconds} milliseconds."
            ];
        }

        if (!string.Equals(watchedFolders.DeletePolicy, MarkDeletedPolicy, StringComparison.OrdinalIgnoreCase))
        {
            errors["watchedFolders.deletePolicy"] = ["Unsupported watched file delete policy."];
        }

        if (watchedFolders.StorageMode != WatchedFolderStorageModes.LinkOnly &&
            watchedFolders.StorageMode != WatchedFolderStorageModes.CopyToAppStorage)
        {
            errors["watchedFolders.storageMode"] = ["Unsupported watched folder storage mode."];
        }

        IReadOnlyList<string> configuredFolderPaths = watchedFolders.Folders
            .Select(folder => folder.Path)
            .ToArray();

        HashSet<string> seenPaths = new(StringComparer.OrdinalIgnoreCase);

        for (int index = 0; index < watchedFolders.Folders.Count; index++)
        {
            WatchedFolderDto folder = watchedFolders.Folders[index];

            if (string.IsNullOrWhiteSpace(folder.Path))
            {
                errors[$"watchedFolders.folders[{index}].path"] = [ErrorMessages.ValueRequired];
                continue;
            }

            string trimmedPath = folder.Path.Trim();

            if (!seenPaths.Add(trimmedPath))
            {
                errors[$"watchedFolders.folders[{index}].path"] = ["Watched folder path is duplicated."];
                continue;
            }

            IReadOnlyList<string> pathErrors = watchedFolderPathValidator.Validate(
                trimmedPath,
                request.RuntimePaths,
                configuredFolderPaths);

            if (pathErrors.Count > 0)
            {
                errors[$"watchedFolders.folders[{index}].path"] = pathErrors.ToArray();
            }
        }
    }

    private static void AddRequired(Dictionary<string, string[]> errors, string key, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors[key] = [ErrorMessages.ValueRequired];
        }
    }
}
