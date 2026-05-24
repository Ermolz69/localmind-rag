namespace KnowledgeApp.Bootstrap.Security;

public sealed class LocalApiSecurityOptions
{
    public const string SectionName = "LocalApi:Security";

    public bool RequireLoopback { get; set; } = true;

    public string? Token { get; set; }

    public long MaxRequestBodyBytes { get; set; } = 100L * 1024 * 1024;
}
