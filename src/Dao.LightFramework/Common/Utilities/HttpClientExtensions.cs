using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

namespace Dao.LightFramework.Common.Utilities;

public static class HttpClientExtensions
{
    public static async Task<string> SendAsync(this HttpClient client, string query, HttpMethod method, object body = null, IEnumerable<KeyValuePair<string, string>> headers = null)

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
        if (method != HttpMethod.Get && body != null)
        {
            request.Content = JsonContent.Create(body, options: new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
            sb.AppendLine("Parameter: " + body.ToJson());
        }

        if (headers != null)
        {
            foreach (var header in headers.Where(w => !string.IsNullOrWhiteSpace(w.Key)))
            {
                request.Headers.Add(header.Key, header.Value);
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
            throw new BadHttpRequestException($"request \"{client.BaseAddress?.ToString().JoinUri(query)}\" failed, response: {result}", (int)(response?.StatusCode ?? 0), ex.GetBaseException());
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

    public static async Task<TResult> SendAsync<TResult>(this HttpClient client, string query, HttpMethod method, object body = null, IEnumerable<KeyValuePair<string, string>> headers = null)
    {
        var result = await client.SendAsync(query, method, body, headers);
        return result.ToObject<TResult>();
    }
}