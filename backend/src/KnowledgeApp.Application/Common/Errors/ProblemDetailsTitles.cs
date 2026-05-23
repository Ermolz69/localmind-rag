namespace KnowledgeApp.Application.Common.Errors;

public static class ProblemDetailsTitles
{
    public const string Conflict = "Request conflicts with current state.";
    public const string ExternalDependencyUnavailable = "External dependency is unavailable.";
    public const string InvalidRequest = "Invalid request.";
    public const string NotFound = "Resource was not found.";
    public const string Unexpected = "An unexpected error occurred.";
    public const string UnsupportedFile = "Unsupported file.";
    public const string ValidationFailed = "Validation failed.";
}
