﻿using System.Collections.Concurrent;
using Dao.LightFramework.Common.Utilities;
using Dao.LightFramework.Services;

namespace Dao.LightFramework.EntityFrameworkCore.DataProviders;

public abstract class WebApiRepository<TDto> : ServiceContextServiceBase, IWebApiRepository<TDto>
{
    readonly string host;
    readonly IHttpClientFactory httpClientFactory;

    protected WebApiRepository(IServiceProvider serviceProvider, string microSvcName) : base(serviceProvider)
    {
        this.httpClientFactory = _<IHttpClientFactory>();
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

    async Task<TResult> Send<TResult>(string query, HttpMethod method, object body)
    {
        Dictionary<string, string> header = null;
        var token = RequestContext.Token;
        if (!string.IsNullOrEmpty(token))
            header = new Dictionary<string, string> { { "Authorization", $"Bearer {token}" } };

        return await HttpClient.SendAsync<TResult>(query, method, body, header);
    }

    readonly ConcurrentDictionary<string, object> queryCache = new(StringComparer.OrdinalIgnoreCase);

    protected virtual async Task<TResult> SendAsync<TResult>(string query, HttpMethod method, object body = null, bool useCache = true)
    {
        if (!useCache)
            return await Send<TResult>(query, method, body);

        var key = new { query, body }.ToJson();
        var result = await this.queryCache.GetOrAddAsync(key, async k => (object)await Send<TResult>(query, method, body));
        return (TResult)result;
    }

    protected async Task<TResult> GetAsync<TResult>(string query, bool useCache = true) => await SendAsync<TResult>(query, HttpMethod.Get, useCache: useCache);

    protected async Task<TResult> PostAsync<TResult>(string query, object body, bool useCache = true) => await SendAsync<TResult>(query, HttpMethod.Post, body, useCache);

    protected async Task<TResult> PutAsync<TResult>(string query, object body, bool useCache = true) => await SendAsync<TResult>(query, HttpMethod.Put, body, useCache);
}