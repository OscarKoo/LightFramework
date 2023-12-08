using Microsoft.AspNetCore.Http;

namespace Dao.LightFramework.Common.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ReadRequestBodyAttribute : Attribute, IMiddlewareAttribute
{
    public Task<object> OnExecutingAsync(HttpContext httpContext, IServiceProvider serviceProvider)
    {
        httpContext.Request.EnableBuffering();
        return Task.FromResult<object>(null);
    }

    public Task OnExecutedAsync(HttpContext httpContext, IServiceProvider serviceProvider, object state) => Task.CompletedTask;
}