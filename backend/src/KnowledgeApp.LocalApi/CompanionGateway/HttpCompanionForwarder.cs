using System.Net.Http.Headers;

namespace KnowledgeApp.LocalApi.CompanionGateway;

/// <summary>
/// Streams an authorized request to the loopback LocalApi and copies the response
/// back (including server-sent events). The phone's device token is stripped and
/// the configured LocalApi token is attached so mutations pass loopback security.
/// </summary>
public sealed class HttpCompanionForwarder(
    HttpClient client,
    string targetBaseUrl,
    string? localApiToken) : ICompanionForwarder
{
    private static readonly HashSet<string> HopByHopHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Connection",
        "Keep-Alive",
        "Proxy-Authenticate",
        "Proxy-Authorization",
        "TE",
        "Trailer",
        "Transfer-Encoding",
        "Upgrade",
        "Host",
        "Content-Length",
    };

    public async Task ForwardAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        HttpRequest source = context.Request;
        string targetUri = $"{targetBaseUrl}{source.Path}{source.QueryString}";

        using HttpRequestMessage request = new(new HttpMethod(source.Method), targetUri);

        bool hasBody = !HttpMethods.IsGet(source.Method)
            && !HttpMethods.IsHead(source.Method)
            && !HttpMethods.IsDelete(source.Method);

        if (hasBody)
        {
            request.Content = new StreamContent(source.Body);
        }

        foreach (var header in source.Headers)
        {
            if (HopByHopHeaders.Contains(header.Key)
                || string.Equals(header.Key, "Authorization", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
            {
                request.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }

        if (!string.IsNullOrWhiteSpace(localApiToken))
        {
            request.Headers.TryAddWithoutValidation("X-LocalMind-Token", localApiToken);
        }

        using HttpResponseMessage response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        context.Response.StatusCode = (int)response.StatusCode;
        CopyResponseHeaders(response.Headers, context.Response);
        CopyResponseHeaders(response.Content.Headers, context.Response);
        context.Response.Headers.Remove("transfer-encoding");

        await using Stream upstream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await upstream.CopyToAsync(context.Response.Body, cancellationToken);
    }

    private static void CopyResponseHeaders(HttpHeaders headers, HttpResponse target)
    {
        foreach (var header in headers)
        {
            if (HopByHopHeaders.Contains(header.Key))
            {
                continue;
            }

            target.Headers[header.Key] = header.Value.ToArray();
        }
    }
}
