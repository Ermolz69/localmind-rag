namespace KnowledgeApp.Infrastructure.Persistence;

internal static class SearchDateIndexing
{
    public const string CreatedAtUnixTimePropertyName = "CreatedAtUnixTimeMs";

    public static long ToUnixTimeMilliseconds(DateTimeOffset value)
    {
        return value.ToUniversalTime().ToUnixTimeMilliseconds();
    }
}
