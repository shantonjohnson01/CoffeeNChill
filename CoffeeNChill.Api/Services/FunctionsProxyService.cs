
using System.Net.Http.Headers;

namespace CoffeeNChill.Api.Services;

public class FunctionsProxyService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public FunctionsProxyService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<HttpResponseMessage> ForwardAsync(
        HttpContext context,
        string downstreamPath)
    {
        var client = _httpClientFactory.CreateClient("Functions");

        string pathAndQuery = downstreamPath +
            context.Request.QueryString.Value;

        var request = new HttpRequestMessage(
            new HttpMethod(context.Request.Method),
            pathAndQuery);

        if (context.Request.ContentLength is > 0 ||
            context.Request.Headers.ContainsKey("Transfer-Encoding"))
        {
            request.Content = new StreamContent(context.Request.Body);

            if (!string.IsNullOrWhiteSpace(context.Request.ContentType))
            {
                request.Content.Headers.TryAddWithoutValidation(
                    "Content-Type", context.Request.ContentType);
            }
        }

        // Forward Accept if supplied by the caller.
        if (context.Request.Headers.TryGetValue("Accept", out var accept))
        {
            request.Headers.TryAddWithoutValidation("Accept", accept.ToArray());
        }

        return await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            context.RequestAborted);
    }
}
