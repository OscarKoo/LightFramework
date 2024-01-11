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

        RequestDelegate nextFunc = async ctx =>
        {
            if (ctx.Response.HasStarted)
                return;

            await this.next(ctx);
        };

        if (!ignore)
        {
            var metadata = httpContext.GetEndpoint()?.Metadata;
            if (metadata != null)
            {
                nextFunc = metadata.Where(w => w is IMiddlewareAttribute).Cast<IMiddlewareAttribute>()
                    .Aggregate(nextFunc, (current, middleware) => BuildNext(middleware, serviceProvider, current));
            }
        }

        await nextFunc(httpContext);
    }

    static RequestDelegate BuildNext(IMiddlewareAttribute middleware, IServiceProvider serviceProvider, RequestDelegate next) =>
        async ctx => await middleware.OnExecutionAsync(ctx, serviceProvider, next);
}