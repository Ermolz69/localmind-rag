using KnowledgeApp.Application.Exceptions;
using KnowledgeApp.Contracts.Settings;
using KnowledgeApp.Domain.Enums;

namespace KnowledgeApp.Application.Settings;

public sealed class SettingsValidator
{
    public void Validate(AppSettingsDto request)
    {
        Dictionary<string, string[]>? errors = new Dictionary<string, string[]>();

        if (!Enum.TryParse<AppTheme>(request.Appearance.Theme, ignoreCase: true, out _))
        {
            errors["appearance.theme"] = ["Theme must be Light, Dark, or System."];
        }

        if (!Enum.TryParse<AiProviderType>(request.Ai.Provider, ignoreCase: true, out _))
        {
            errors["ai.provider"] = ["AI provider must be Ollama or LlamaCpp."];
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

        if (errors.Count > 0)
        {
            throw new ValidationAppException("Settings validation failed.", "settings.validationFailed", errors);
        }
    }

    private static void AddRequired(Dictionary<string, string[]> errors, string key, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors[key] = ["Value is required."];
        }
    }
}
