using Microsoft.AspNetCore.Http;

namespace Dao.LightFramework.Common.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ReadRequestBodyAttribute : Attribute, IMiddlewareAttribute
{
    public async Task OnExecutionAsync(HttpContext httpContext, IServiceProvider serviceProvider, RequestDelegate next)
    {
        httpContext.Request.EnableBuffering();
        await next(httpContext);
    }
}