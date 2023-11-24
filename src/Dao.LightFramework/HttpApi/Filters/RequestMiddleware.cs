using Dao.LightFramework.Common.Attributes;
using Dao.LightFramework.Common.Utilities;
using Microsoft.AspNetCore.Http;

namespace Dao.LightFramework.HttpApi.Filters;

public class RequestMiddleware
{
    readonly RequestDelegate next;

    public RequestMiddleware(RequestDelegate next) => this.next = next;

    public async Task InvokeAsync(HttpContext httpContext, IServiceProvider serviceProvider)
    {
        var method = httpContext.Request.Method;
        var ignore = method.EqualsIgnoreCase("OPTIONS") || method.EqualsIgnoreCase("HEAD") || method.EqualsIgnoreCase("TRACE");
        var middlewares = new List<MiddlewareState>();

        if (!ignore)
        {
            var metadata = httpContext.GetEndpoint()?.Metadata;
            if (metadata != null)
            {
                middlewares.AddRange(metadata.Where(w => w is IMiddlewareAttribute).Select(s => new MiddlewareState((IMiddlewareAttribute)s)));

                foreach (var middleware in middlewares)
                {
                    middleware.State = await middleware.Middleware.OnExecutingAsync(httpContext, serviceProvider);
                    if (httpContext.Response.HasStarted)
                        return;
                }
            }
        }

        await this.next(httpContext);

        if (!ignore)
        {
            foreach (var middleware in ((IList<MiddlewareState>)middlewares).Reverse())
            {
                await middleware.Middleware.OnExecutedAsync(httpContext, serviceProvider, middleware.State);
            }
        }
    }
}

sealed class MiddlewareState
{
    internal MiddlewareState(IMiddlewareAttribute middleware) => Middleware = middleware;

    internal IMiddlewareAttribute Middleware { get; set; }
    internal object State { get; set; }
}