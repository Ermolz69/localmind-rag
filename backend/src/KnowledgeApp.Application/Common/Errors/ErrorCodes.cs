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

    public static class Ingestion
    {
        public const string JobNotFound = "INGESTION_JOB_NOT_FOUND";
        public const string JobNotRetryable = "INGESTION_JOB_NOT_RETRYABLE";
        public const string JobNotCancellable = "INGESTION_JOB_NOT_CANCELLABLE";
        public const string JobAlreadyRunning = "INGESTION_JOB_ALREADY_RUNNING";
        public const string JobFailed = "INGESTION_JOB_FAILED";
    }

    public static class Security
    {
        public const string LocalAccessDenied = "LOCAL_ACCESS_DENIED";
        public const string LocalTokenRequired = "LOCAL_TOKEN_REQUIRED";
        public const string LocalTokenInvalid = "LOCAL_TOKEN_INVALID";
        public const string RequestTooLarge = "REQUEST_TOO_LARGE";
        public const string UnsupportedMediaType = "UNSUPPORTED_MEDIA_TYPE";
    }

    public static class Settings
    {
        public const string ValidationFailed = "VALIDATION_FAILED";
    }
}
