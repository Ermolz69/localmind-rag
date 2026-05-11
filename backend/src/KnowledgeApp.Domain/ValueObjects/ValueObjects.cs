namespace KnowledgeApp.Domain.ValueObjects;

public readonly record struct ChunkText(string Value);
public readonly record struct ContentHash(string Value);
public readonly record struct DeviceId(string Value);
public readonly record struct EmbeddingVector(float[] Values);
public readonly record struct LocalFilePath(string Value);
public readonly record struct ModelName(string Value);
public readonly record struct RemoteFileId(string Value);
public readonly record struct SyncCursor(string Value);
