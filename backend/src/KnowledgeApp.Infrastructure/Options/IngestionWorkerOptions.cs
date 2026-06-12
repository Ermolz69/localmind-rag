namespace KnowledgeApp.Infrastructure.Options;

public sealed class IngestionWorkerOptions
{
    public const string SectionName = "IngestionWorker";

    public bool Enabled { get; set; }

    public int RecoveryIntervalSeconds { get; set; } = 60;

    public int RecoveryBatchSize { get; set; } = 100;
}
