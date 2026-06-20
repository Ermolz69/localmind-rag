namespace KnowledgeApp.Application.Common.Errors;

public static class ErrorMessages
{
    public const string UnexpectedDevelopment = "An unexpected error occurred.";
    public const string UnexpectedProduction = "The server encountered an unexpected error.";
    public const string ValueRequired = "Value is required.";

    public static class Buckets
    {
        public const string DescriptionTooLong = "Bucket description must be 500 characters or less.";
        public const string NameRequired = "Bucket name is required.";
        public const string NameTooLong = "Bucket name must be 120 characters or less.";
        public const string NotFound = "Selected bucket was not found.";
        public const string RequestInvalid = "Bucket request is invalid.";
    }

    public static class Chats
    {
        public const string ContentRequired = "Chat message content is required.";
        public const string ContentTooLong = "Chat message content must be 20000 characters or less.";
        public const string NotFound = "Conversation was not found.";
        public const string RequestInvalid = "Chat request is invalid.";
        public const string TitleRequired = "Chat title is required.";
        public const string TitleTooLong = "Chat title must be 200 characters or less.";
    }

    public static class Documents
    {
        public const string FileEmpty = "Document file must not be empty.";
        public const string FileNameRequired = "Document file name is required.";
        public const string FileTooLarge = "Document file size must be less than or equal to 100 MB.";
        public const string InvalidStatus = "Document status filter is invalid.";
        public const string PreviewFileMissing = "Document preview file is unavailable.";
        public const string PreviewUnavailable = "Document preview is unavailable.";
        public const string PreviewUnsupported = "Document preview is not supported for this file type.";
        public const string PreviewConverterUnavailable = "Document preview converter is unavailable.";
        public const string PreviewConversionFailed = "Document preview conversion failed.";
        public const string PreviewConversionTimeout = "Document preview conversion timed out.";
        public const string UnsupportedFileType = "Document file extension is not supported.";
    }

    public static class Notes
    {
        public const string MarkdownTooLong = "Note markdown must be 1000000 characters or less.";
        public const string RequestInvalid = "Note request is invalid.";
        public const string TitleRequired = "Note title is required.";
        public const string TitleTooLong = "Note title must be 200 characters or less.";
    }

    public static class Pagination
    {
        public const string InvalidCursor = "Cursor is invalid for this request.";
        public const string InvalidLimit = "Cursor page limit must be between 1 and 200.";
        public const string LimitOutOfRange = "Limit must be between 1 and 200.";
    }

    public static class Search
    {
        public const string LimitOutOfRange = "Search limit must be between 1 and 50.";
        public const string QueryRequired = "Search query is required.";
        public const string QueryTooLong = "Search query must be 4000 characters or less.";
        public const string RequestInvalid = "Semantic search request is invalid.";
    }

    public static class Runtime
    {
        public const string ExternalDependencyUnavailable = "Local AI runtime dependency is unavailable.";
        public const string AiProviderNotFound = "Configured AI provider was not found.";
        public const string AiProviderUnavailable = "Configured AI provider is unavailable.";
        public const string AiProviderCapabilityUnsupported = "Configured AI provider does not support the requested capability.";
        public const string AiRuntimeUnavailable = "AI runtime is unavailable.";
        public const string AiModelNotFound = "AI model was not found.";
    }

    public static class Ingestion
    {
        public const string JobNotFound = "Ingestion job was not found.";
        public const string JobNotRetryable = "Ingestion job cannot be retried in its current state.";
        public const string JobNotCancellable = "Ingestion job cannot be cancelled in its current state.";
        public const string JobAlreadyRunning = "Ingestion job is already running.";
        public const string JobFailed = "Ingestion job failed.";
        public const string RetryQueued = "Ingestion job was queued for retry.";
        public const string Cancelled = "Ingestion job was cancelled.";
        public const string ProcessingAccepted = "Ingestion job processing was accepted.";
    }

    public static class Security
    {
        public const string LocalAccessDenied = "LocalApi accepts local desktop requests only.";
        public const string LocalTokenRequired = "Local API token is required.";
        public const string LocalTokenInvalid = "Local API token is invalid.";
        public const string RequestTooLarge = "Request body is too large.";
        public const string UnsupportedMediaType = "Request content type is not supported.";
    }

    public static class Settings
    {
        public const string AiProviderInvalid = "AI provider must be Ollama or LlamaCpp.";
        public const string ThemeInvalid =
            "Theme must be Light, Dark, System, GraphiteBlue, MidnightViolet, SlateTealAmber, or CarbonGrayBlue.";
        public const string ValidationFailed = "Settings validation failed.";
    }
}
