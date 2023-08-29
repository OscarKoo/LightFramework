using Dao.LightFramework.Common.Attributes;
using Microsoft.AspNetCore.Http;

namespace Dao.LightFramework.HttpApi.Filters;

public class RequestMiddleware
{
    readonly RequestDelegate next;

    public RequestMiddleware(RequestDelegate next) => this.next = next;

    public async Task InvokeAsync(HttpContext httpContext)
    {
        if (ReadRequestBodyAttribute.Enabled)
        {
            var readBody = httpContext.GetEndpoint()?.Metadata.GetMetadata<ReadRequestBodyAttribute>();
            if (readBody != null)
                httpContext.Request.EnableBuffering();
        }

        await this.next(httpContext);
    }
}