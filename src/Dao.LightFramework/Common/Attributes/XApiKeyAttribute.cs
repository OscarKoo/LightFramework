using Dao.LightFramework.Common.Utilities;
using IdentityModel.Client;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace Dao.LightFramework.Common.Attributes;

public class XApiKeyAttribute : Attribute, IAsyncActionFilterAttribute
{
    public string HeaderKey { get; set; } = "X-API-KEY";
    public string ParameterKey { get; set; } = "apiKey";
    public string ConfigKey { get; set; } = "ApiKey";

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
            var apiKey = headers[HeaderKey].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(apiKey))
                apiKey = request.Query[ParameterKey];
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new UnauthorizedAccessException("ApiKey not provided.");

            var apiKeyValue = serviceProvider.GetRequiredService<IConfiguration>().GetSection(ConfigKey).Value;
            if (string.IsNullOrWhiteSpace(apiKeyValue) || !string.Equals(apiKey, apiKeyValue, StringComparison.Ordinal))
                throw new UnauthorizedAccessException("ApiKey not set or not match.");

            token = await authenticator.GenerateToken(null, null);
            if (string.IsNullOrWhiteSpace(token))
                throw new UnauthorizedAccessException("Unable to generate access_token!");

            headers.Authorization = new StringValues("Bearer " + token);
        }

        StaticLogger.LogInformation(@$"XApiKey validation: {request.Scheme}://{request.Host}{request.Path}{request.QueryString.Value}
Cost: {sw.Stop()}");
        return null;
    }

    public Task OnActionExecutedAsync(ActionExecutingContext executingContext, IServiceProvider serviceProvider, ActionExecutedContext executedContext, object state) => Task.CompletedTask;
}

public interface IAuthenticator
{
    Task<bool> Authenticate(string clientId, string secret, string token, Parameters parameters = default);
    Task<string> GenerateToken(string clientId, string secret, Parameters parameters = default);
}