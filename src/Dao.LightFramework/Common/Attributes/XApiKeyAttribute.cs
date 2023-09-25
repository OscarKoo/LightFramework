using Dao.LightFramework.Common.Utilities;
using IdentityModel.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;

namespace Dao.LightFramework.Common.Attributes;

public class XApiKeyAttribute : Attribute, IAsyncActionFilterAttribute
{
    public string HeaderKey { get; set; } = "X-API-KEY";
    public string ParameterKey { get; set; } = "apiKey";
    public string ConfigKey { get; set; } = "ApiKey";
    public string OperatorKey { get; set; }

    public async Task<object> OnActionExecutingAsync(ActionExecutingContext executingContext, IServiceProvider serviceProvider)
    {
        var sw = new StopWatch();
        sw.Start();

        var authenticator = serviceProvider.GetRequiredService<IAuthenticator>();

        var request = executingContext.HttpContext.Request;
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
            var query = request.Query;
            var args = executingContext.ActionArguments.JsonCopy() as JToken;

            var apiKeys = (string.IsNullOrWhiteSpace(HeaderKey) ? Enumerable.Empty<string>() : headers[HeaderKey])
                .Concat(GetQueryString(ParameterKey, query, args))
                .Where(w => !string.IsNullOrWhiteSpace(w)).ToList();

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
            if (!string.IsNullOrWhiteSpace(OperatorKey))
            {
                var opt = GetQueryString(OperatorKey, query, args).FirstOrDefault(w => !string.IsNullOrWhiteSpace(w));
                if (!string.IsNullOrWhiteSpace(opt))
                    parameters.Add("Operator", opt);
            }

            token = await authenticator.GenerateToken(null, null, parameters);
            if (string.IsNullOrWhiteSpace(token))
                throw new UnauthorizedAccessException("Unable to generate access_token!");

            headers.Authorization = new StringValues("Bearer " + token);
        }

        StaticLogger.LogInformation(@$"XApiKey validation: {request.Scheme}://{request.Host}{request.Path}{request.QueryString.Value}
Cost: {sw.Stop()}");
        return null;
    }

    static IEnumerable<string> GetQueryString(string name, IQueryCollection query, JToken args) =>
        string.IsNullOrWhiteSpace(name)
            ? Array.Empty<string>()
            : query[name].Concat(args.GetValues<string>(name, StringComparison.OrdinalIgnoreCase));

    public Task OnActionExecutedAsync(ActionExecutingContext executingContext, IServiceProvider serviceProvider, ActionExecutedContext executedContext, object state) => Task.CompletedTask;
}

public interface IAuthenticator
{
    Task<bool> Authenticate(string clientId, string secret, string token, Parameters parameters = default);
    Task<string> GenerateToken(string clientId, string secret, Parameters parameters = default);
}