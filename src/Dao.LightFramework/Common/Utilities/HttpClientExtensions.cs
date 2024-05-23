﻿using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dao.LightFramework.Traces;
using Microsoft.AspNetCore.Http;

namespace Dao.LightFramework.Common.Utilities;

public static class HttpClientExtensions
{
    static async Task<TResult> SendCoreAsync<TResult>(this HttpClient client, string query, HttpMethod method, HttpContent content = null, IEnumerable<KeyValuePair<string, string>> headers = null)
    {
        if (client == null)
            return default;
        if (client.Timeout != Timeout.InfiniteTimeSpan)
            client.Timeout = Timeout.InfiniteTimeSpan;

        var uri = !string.IsNullOrWhiteSpace(query)
            ? new Uri(query, UriKind.RelativeOrAbsolute)
            : null;

        var url = (client.BaseAddress?.ToString()).JoinUri(query);
        var sb = new StringBuilder();
        sb.AppendLine($"({DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}) MicroService: {method.ToString()} {url}");

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
        TResult result = default;
        string error = null;
        try
        {
            response = await client.SendAsync(request);

            var type = typeof(TResult);
            result = type == typeof(string)
                ? (await response.Content.ReadAsStringAsync()).Cast<string, TResult>()
                : type == typeof(Stream)
                    ? (await response.Content.ReadAsStreamAsync()).Cast<Stream, TResult>()
                    : type == typeof(byte[])
                        ? (await response.Content.ReadAsByteArrayAsync()).Cast<byte[], TResult>()
                        : throw new NotSupportedException($"{type.Name} is not supported.");

            response.EnsureSuccessStatusCode();
            return result;
        }
        catch (Exception ex)
        {
            error = ex.GetBaseException().Message;
            throw new BadHttpRequestException($"request \"{url}\" failed, response: {ReadResultString(result)}, error: {error}", (int)(response?.StatusCode ?? 0));
        }
        finally
        {
            sb.AppendLine($"Result: {ReadResultString(result) ?? error}");
            sb.AppendLine($"Response: Cost {sw.Stop()}");

            if (string.IsNullOrWhiteSpace(error))
                StaticLogger.LogInformation(sb.ToString());
            else
                StaticLogger.LogError(sb.ToString());
        }
    }

    static string ReadResultString<TResult>(TResult result)
    {
        if (result == null)
            return null;

        var type = typeof(TResult);
        return type == typeof(string) ? result as string : type.Name;
    }

    public static async Task<string> SendAsync(this HttpClient client, string query, HttpMethod method, HttpContent content = null, IEnumerable<KeyValuePair<string, string>> headers = null) =>
        await client.SendCoreAsync<string>(query, method, content, headers);

    public static async Task<string> SendAsync(this HttpClient client, string query, HttpMethod method, object body = null, IEnumerable<KeyValuePair<string, string>> headers = null) =>
        await client.SendAsync(query, method, JsonContent.Create(body, options: new JsonSerializerOptions(JsonSerializerDefaults.Web) { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull }), headers);

    public static async Task<TResult> SendAsync<TResult>(this HttpClient client, string query, HttpMethod method, HttpContent content = null, IEnumerable<KeyValuePair<string, string>> headers = null)
    {
        var type = typeof(TResult);
        return type == typeof(string) || type == typeof(Stream) || type == typeof(byte[])
            ? await client.SendCoreAsync<TResult>(query, method, content, headers)
            : (await client.SendAsync(query, method, content, headers)).ToObject<TResult>();
    }

    public static async Task<TResult> SendAsync<TResult>(this HttpClient client, string query, HttpMethod method, object body = null, IEnumerable<KeyValuePair<string, string>> headers = null) =>
        await client.SendAsync<TResult>(query, method, JsonContent.Create(body, options: new JsonSerializerOptions(JsonSerializerDefaults.Web) { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull }), headers);

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