using Dao.LightFramework.Common.Utilities;
using Dao.LightFramework.Traces;
using IdentityModel.Client;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace Dao.LightFramework.Common.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class XApiKeyAttribute : Attribute, IAsyncActionFilterAttribute
{
    public XApiKeyAttribute(params string[] tokenClaimKeys) => TokenClaimKeys = tokenClaimKeys;

    public bool Disabled { get; set; }

    public string HeaderKey { get; set; } = "X-API-KEY";
    public string ParameterKey { get; set; } = "apiKey";
    public string ConfigKey { get; set; } = "ApiKey";
    public string[] TokenClaimKeys { get; set; }

    public async Task<ActionExecutedContext> OnActionExecutionAsync(ActionExecutingContext context, IServiceProvider serviceProvider, ActionExecutionDelegate next)
    {
        var sw = new StopWatch();
        sw.Start();

        var authenticator = serviceProvider.GetRequiredService<IAuthenticator>();

        var request = context.HttpContext.Request;
        var headers = request.Headers;
        var token = headers.Authorization.FirstOrDefault();
        var isValid = false;
        if (!string.IsNullOrWhiteSpace(token))
        {
            var auth = token.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (auth.Length == 2)
            {
                token = auth[1];
                isValid = await authenticator.Authenticate(null, null, token);
            }
        }

        if (!isValid)
        {
            var apiKeys = context.GetRequestParameter(HeaderKey, ParameterKey, true).ToList();

            if (apiKeys.IsNullOrEmpty())
                throw new UnauthorizedAccessException($"Header \"{HeaderKey}\" or Parameter \"{ParameterKey}\" not provided.");

            var apiKeyValues = string.IsNullOrWhiteSpace(ConfigKey)
                ? Array.Empty<string>()
                : serviceProvider.GetRequiredService<IConfiguration>().GetSection(ConfigKey).Value?.Split(new[] { ',', '|' }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (apiKeyValues.IsNullOrEmpty())
                throw new UnauthorizedAccessException($"Config \"{ConfigKey}\" not set yet.");

            if (!apiKeyValues!.Any(v => apiKeys.Any(k => string.Equals(k, v, StringComparison.Ordinal))))
                throw new UnauthorizedAccessException("ApiKey not match.");

            var parameters = new Parameters();
            if (!TokenClaimKeys.IsNullOrEmpty())
            {
                foreach (var key in TokenClaimKeys)
                {
                    var value = context.GetRequestParameter(null, key, true).FirstOrDefault(w => !string.IsNullOrWhiteSpace(w));
                    if (!string.IsNullOrWhiteSpace(value))
                        parameters.Add(key, value);
                }
            }

            token = await authenticator.GenerateToken(null, null, parameters);
            if (string.IsNullOrWhiteSpace(token))
                throw new UnauthorizedAccessException("Unable to generate access_token!");

            headers.Authorization = new StringValues("Bearer " + token);
        }

        StaticLogger.LogInformation(@$"({TraceContext.TraceId.Value}) XApiKey validation: {request.Method} {request.Scheme}://{request.Host}{request.Path}{request.QueryString.Value}
Elapsed: Cost: {sw.Stop()}");

        return await next();
    }
}

public interface IAuthenticator
{
    Task<bool> Authenticate(string clientId, string secret, string token, Parameters parameters = default);
    Task<string> GenerateToken(string clientId, string secret, Parameters parameters = default);
}