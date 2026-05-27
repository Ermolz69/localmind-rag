namespace KnowledgeApp.Infrastructure.Options.Validation;

internal static class OptionsValidationRules
{
    internal static bool IsMissing(string? value)
    {
        return string.IsNullOrWhiteSpace(value);
    }

    internal static bool IsAbsoluteHttpUrl(string? value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out Uri? uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    internal static bool IsValidPath(string? value)
    {
        if (IsMissing(value))
        {
            return false;
        }

        try
        {
            _ = Path.GetFullPath(value!);

            return true;
        }
        catch (Exception exception) when (
            exception is ArgumentException
            or NotSupportedException
            or PathTooLongException)
        {
            return false;
        }
    }
}
