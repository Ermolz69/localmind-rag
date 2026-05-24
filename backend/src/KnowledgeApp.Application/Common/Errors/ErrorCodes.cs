namespace KnowledgeApp.Application.Common.Errors;

public static class ErrorCodes
{
    public const string Unexpected = "INTERNAL_SERVER_ERROR";
    public const string RequestInvalid = "REQUEST_INVALID";

    public static class Buckets
    {
        public const string ValidationFailed = "VALIDATION_FAILED";
        public const string NotFound = "BUCKET_NOT_FOUND";
    }

    public static class Chats
    {
        public const string ValidationFailed = "VALIDATION_FAILED";
        public const string NotFound = "CHAT_NOT_FOUND";
    }

    public static class Documents
    {
        public const string FileEmpty = "VALIDATION_FAILED";
        public const string FileNameRequired = "VALIDATION_FAILED";
        public const string FileTooLarge = "VALIDATION_FAILED";
        public const string InvalidStatus = "VALIDATION_FAILED";
        public const string NotFound = "DOCUMENT_NOT_FOUND";
        public const string UnsupportedFileType = "VALIDATION_FAILED";
    }

    public static class Notes
    {
        public const string ValidationFailed = "VALIDATION_FAILED";
        public const string NotFound = "NOTE_NOT_FOUND";
    }

    public static class Pagination
    {
        public const string InvalidCursor = "VALIDATION_FAILED";
        public const string InvalidLimit = "VALIDATION_FAILED";
    }

    public static class Search
    {
        public const string ValidationFailed = "VALIDATION_FAILED";
    }

    public static class Runtime
    {
        public const string ExternalDependencyUnavailable = "EXTERNAL_DEPENDENCY_UNAVAILABLE";
    }

    public static class Settings
    {
        public const string ValidationFailed = "VALIDATION_FAILED";
    }
}
