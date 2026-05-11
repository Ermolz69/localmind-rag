namespace KnowledgeApp.Domain.Enums;

public enum AiProviderType { Ollama, LlamaCpp }
public enum AiRuntimeStatus { Unknown, Stopped, Starting, Running, Missing, Failed }
public enum AppTheme { Light, Dark, System }
public enum ChatRole { System, User, Assistant, Tool }
public enum DocumentStatus { Draft, Uploaded, Queued, Processing, Indexed, Failed, Deleted }
public enum FileType { Unknown, Pdf, Docx, Pptx, Markdown, PlainText, Html }
public enum IngestionJobStatus { Queued, Running, Completed, Failed, Cancelled }
public enum SyncDirection { Upload, Download }
public enum SyncOperation { CreateDocument, UpdateDocument, DeleteDocument, UploadFile, CreateNote, UpdateNote, DeleteNote, CreateBucket, UpdateBucket, DeleteBucket }
public enum SyncStatus { LocalOnly, PendingUpload, Uploading, Uploaded, PendingDownload, Downloading, Downloaded, Synced, Conflict, UploadFailed, DownloadFailed, DeletedLocal, DeletedRemote }
public enum VectorIndexProvider { ExactSqlite, SqliteVec, Sidecar }
