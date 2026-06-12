namespace KnowledgeApp.Infrastructure.Services.Search;

internal static class SearchDateRange
{
    public static DateTimeOffset ToInclusiveEndOfDay(DateTimeOffset value)
    {
        return new DateTimeOffset(value.Date, value.Offset)
            .AddDays(1)
            .AddTicks(-1);
    }
}
