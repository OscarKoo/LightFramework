using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dao.LightFramework.Traces;
using Microsoft.AspNetCore.Http;

namespace Dao.LightFramework.Common.Utilities;

public static class HttpClientExtensions
{
    public static async Task<string> SendAsync(this HttpClient client, string query, HttpMethod method, HttpContent content = null, IEnumerable<KeyValuePair<string, string>> headers = null)
    {
        if (client == null)
            return default;
        if (client.Timeout != Timeout.InfiniteTimeSpan)
            client.Timeout = Timeout.InfiniteTimeSpan;

        var uri = !string.IsNullOrWhiteSpace(query)
            ? new Uri(query, UriKind.RelativeOrAbsolute)
            : null;

        var sb = new StringBuilder();
        sb.AppendLine($"({DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}) MicroService: {client.BaseAddress?.Scheme}://{client.BaseAddress?.Authority}/{uri?.OriginalString}");

        var request = new HttpRequestMessage(method, uri);
        if (method != HttpMethod.Get && content != null)
        {
            request.Content = content;
            sb.AppendLine("Parameter: " + await content.ReadAsStringAsync());
        }

        request.Headers.RemoveNames(TraceId.Header, SpanId.Header);
        var traceId = TraceContext.TraceId.Value;
        if (!string.IsNullOrWhiteSpace(traceId))
            request.Headers.Add(TraceId.Header, traceId);
        if (TraceContext.SpanId.HasValue)
        {
            var spanId = TraceContext.SpanId.Value;
            request.Headers.Add(SpanId.Header, spanId);
        }

        if (headers != null)
        {
            foreach (var header in headers.Where(w => !string.IsNullOrWhiteSpace(w.Key)))
            {
                request.Headers.Set(header.Key, header.Value);
            }
        }

        var sw = new StopWatch();
        sw.Start();

        HttpResponseMessage response = null;
        string result = null;
        string error = null;
        try
        {
            response = await client.SendAsync(request);
            result = await response.Content.ReadAsStringAsync();
            response.EnsureSuccessStatusCode();
            return result;
        }
        catch (Exception ex)
        {
            error = ex.GetBaseException().Message;
            throw new BadHttpRequestException($"request \"{(client.BaseAddress?.ToString()).JoinUri(query)}\" failed, response: {result}, error: {error}", (int)(response?.StatusCode ?? 0));
        }
        finally
        {
            sb.AppendLine($"Result: {result ?? error}");
            sb.AppendLine($"Response: Cost {sw.Stop()}");

            if (string.IsNullOrWhiteSpace(error))
                StaticLogger.LogInformation(sb.ToString());
            else
                StaticLogger.LogError(sb.ToString());
        }
    }

    public static async Task<string> SendAsync(this HttpClient client, string query, HttpMethod method, object body = null, IEnumerable<KeyValuePair<string, string>> headers = null) =>
        await client.SendAsync(query, method, JsonContent.Create(body, options: new JsonSerializerOptions(JsonSerializerDefaults.Web) { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull }), headers);

    public static async Task<TResult> SendAsync<TResult>(this HttpClient client, string query, HttpMethod method, HttpContent content = null, IEnumerable<KeyValuePair<string, string>> headers = null)
    {
        var result = await client.SendAsync(query, method, content, headers);
        return result.ToObject<TResult>();
    }

    public static async Task<TResult> SendAsync<TResult>(this HttpClient client, string query, HttpMethod method, object body = null, IEnumerable<KeyValuePair<string, string>> headers = null)
    {
        var result = await client.SendAsync(query, method, body, headers);
        return result.ToObject<TResult>();
    }

    public static void RemoveNames(this HttpHeaders headers, params string[] names)
    {
        if (headers == null || names.IsNullOrEmpty())
            return;

        foreach (var name in names)
        {
            if (headers.Contains(name))
                headers.Remove(name);
        }
    }

    public static void Set(this HttpHeaders headers, string name, string value)
    {
        if (headers == null)
            return;
        headers.RemoveNames(name);
        headers.Add(name, value);
    }
}