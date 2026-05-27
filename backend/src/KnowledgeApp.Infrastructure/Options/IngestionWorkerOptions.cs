namespace KnowledgeApp.Infrastructure.Options;

public sealed class IngestionWorkerOptions
{
    public const string SectionName = "IngestionWorker";

    public bool Enabled { get; set; }

    public int PollIntervalSeconds { get; set; } = 2;

    public int BatchSize { get; set; } = 1;
}
