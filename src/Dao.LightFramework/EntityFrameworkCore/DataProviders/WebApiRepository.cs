using Dao.LightFramework.Common.Utilities;
using Dao.LightFramework.Services;
using Dao.LightFramework.Services.Contexts;

namespace Dao.LightFramework.EntityFrameworkCore.DataProviders;

public abstract class WebApiRepository : ServiceContextServiceBase, IWebApiRepository
{
    readonly string host;
    readonly IHttpClientFactory httpClientFactory;
    readonly string microSvcName;

    protected WebApiRepository(IServiceProvider serviceProvider, string microSvcName) : base(serviceProvider)
    {
        this.httpClientFactory = _<IHttpClientFactory>();
        this.microSvcName = microSvcName;
        if (!string.IsNullOrWhiteSpace(microSvcName))
        {
            this.host = _<IServiceDiscovery>().FindService(microSvcName).GetAwaiter().GetResult();
            if (string.IsNullOrWhiteSpace(this.host))
                throw new Exception($"Unable to connect to micro service \"{ServiceName}\"");
        }
    }

    HttpClient httpClient;
    protected HttpClient HttpClient
    {
        get
        {
            if (this.httpClient == null)
            {
                if (string.IsNullOrWhiteSpace(this.host))
                    return null;

                this.httpClient = this.httpClientFactory.CreateClient();
                this.httpClient.BaseAddress = new Uri(this.host);
            }

            return this.httpClient;
        }
    }

    async Task<TResult> Send<TResult>(string query, HttpMethod method, object body, IDictionary<string, string> headers = null)
    {
        var token = RequestContext.Token;
        if (!string.IsNullOrEmpty(token))
        {
            headers ??= new Dictionary<string, string>();
            headers["Authorization"] = $"Bearer {token}";
        }

        return await HttpClient.SendAsync<TResult>(query, method, body, headers);
    }

    protected virtual async Task<TResult> SendAsync<TResult>(string query, HttpMethod method, object body = null, bool? useCache = null, IDictionary<string, string> headers = null)
    {
        useCache ??= method == HttpMethod.Get;
        if (!useCache.Value)
            return await Query();

        var key = new { service = this.microSvcName, query, body }.ToJson();
        return await MicroServiceContext.GetCacheOrQueryAsync(key, Query);

        async Task<TResult> Query() => await Send<TResult>(query, method, body, headers);
    }

    protected virtual async Task<TResult> GetAsync<TResult>(string query, bool? useCache = null, IDictionary<string, string> headers = null) => await SendAsync<TResult>(query, HttpMethod.Get, useCache: useCache, headers: headers);

    protected virtual async Task<TResult> PostAsync<TResult>(string query, object body, bool? useCache = null, IDictionary<string, string> headers = null) => await SendAsync<TResult>(query, HttpMethod.Post, body, useCache, headers);

    protected virtual async Task<TResult> PutAsync<TResult>(string query, object body, bool? useCache = null, IDictionary<string, string> headers = null) => await SendAsync<TResult>(query, HttpMethod.Put, body, useCache, headers);
}