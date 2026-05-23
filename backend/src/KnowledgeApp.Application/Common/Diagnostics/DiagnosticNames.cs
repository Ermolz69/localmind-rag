namespace KnowledgeApp.Application.Common.Diagnostics;

public static class DiagnosticNames
{
    public static class Areas
    {
        public const string Documents = "documents";
        public const string Ingestion = "ingestion";
        public const string Rag = "rag";
        public const string Runtime = "runtime";
        public const string Search = "search";
        public const string Sync = "sync";
    }

    public static class Operations
    {
        public const string AiRuntimeSetup = "ai-setup";
        public const string AiRuntimeStart = "ai-start";
        public const string BuildContext = "build-context";
        public const string DocumentUpload = "upload";
        public const string IngestionDispatch = "dispatch";
        public const string IngestionProcessJob = "process-job";
        public const string RagAnswer = "answer";
        public const string SemanticSearch = "semantic";
        public const string SyncRun = "run";
        public const string SyncStatus = "status";
    }

    public static class Steps
    {
        public const string AnswerGenerated = "answer-generated";
        public const string AutostartDisabled = "autostart-disabled";
        public const string ChunksAndEmbeddingsCreated = "chunks-and-embeddings-created";
        public const string ContextBuilt = "context-built";
        public const string DispatchCompleted = "dispatch-completed";
        public const string DocumentCreated = "document-created";
        public const string DocumentNotFound = "document-not-found";
        public const string JobClaimSkipped = "job-claim-skipped";
        public const string JobFinished = "job-finished";
        public const string JobNotFound = "job-not-found";
        public const string JobSkipped = "job-skipped";
        public const string ModelMissing = "model-missing";
        public const string ModelReady = "model-ready";
        public const string RuntimeAlreadyRunning = "already-running";
        public const string RuntimeMissing = "runtime-missing";
        public const string RuntimeReady = "runtime-ready";
        public const string RuntimeStartFailed = "runtime-started/failed";
        public const string RuntimeStartFinished = "runtime-started";
        public const string RunAccepted = "run-accepted";
        public const string SemanticSearchCompleted = "semantic-search-completed";
        public const string SemanticSearchFailed = "semantic-search-failed";
        public const string SemanticSearchStarted = "semantic-search-started";
        public const string StatusReturned = "status-returned";
        public const string TextExtracted = "text-extracted";
        public const string UploadSaved = "upload-saved";
    }

    public static class Properties
    {
        public const string AnswerLength = "AnswerLength";
        public const string BaseUrl = "BaseUrl";
        public const string BucketId = "BucketId";
        public const string ChunksCount = "ChunksCount";
        public const string ContextLength = "ContextLength";
        public const string ConversationId = "ConversationId";
        public const string DocumentId = "DocumentId";
        public const string DocumentStatus = "DocumentStatus";
        public const string EmbeddingsCount = "EmbeddingsCount";
        public const string FileExtension = "FileExtension";
        public const string FileName = "FileName";
        public const string IngestionJobId = "IngestionJobId";
        public const string JobId = "JobId";
        public const string Length = "Length";
        public const string Limit = "Limit";
        public const string ModelPath = "ModelPath";
        public const string RuntimePath = "RuntimePath";
        public const string SegmentsCount = "SegmentsCount";
        public const string SourcesCount = "SourcesCount";
        public const string Status = "Status";
    }
}
