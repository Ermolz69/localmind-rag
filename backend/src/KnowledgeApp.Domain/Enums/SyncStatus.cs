namespace KnowledgeApp.Domain.Enums;

public enum SyncStatus { LocalOnly, PendingUpload, Uploading, Uploaded, PendingDownload, Downloading, Downloaded, Synced, Conflict, UploadFailed, DownloadFailed, DeletedLocal, DeletedRemote }

