namespace KnowledgeApp.Application.Common.Errors;

public static class ErrorCodes
{
    public const string Unexpected = "errors.unexpected";
    public const string RequestInvalid = "request.invalid";

    public static class Buckets
    {
        public const string ValidationFailed = "buckets.validationFailed";
        public const string NotFound = "buckets.notFound";
    }

    public static class Chats
    {
        public const string ValidationFailed = "chats.validationFailed";
        public const string NotFound = "chats.notFound";
    }

    public static class Documents
    {
        public const string FileEmpty = "documents.fileEmpty";
        public const string FileNameRequired = "documents.fileNameRequired";
        public const string FileTooLarge = "documents.fileTooLarge";
        public const string InvalidStatus = "documents.invalidStatus";
        public const string UnsupportedFileType = "documents.unsupportedFileType";
    }

    public static class Notes
    {
        public const string ValidationFailed = "notes.validationFailed";
    }

    public static class Pagination
    {
        public const string InvalidCursor = "pagination.invalidCursor";
        public const string InvalidLimit = "pagination.invalidLimit";
    }

    public static class Search
    {
        public const string ValidationFailed = "search.validationFailed";
    }

    public static class Runtime
    {
        public const string ExternalDependencyUnavailable = "runtime.externalDependencyUnavailable";
    }

    public static class Settings
    {
        public const string ValidationFailed = "settings.validationFailed";
    }
}
