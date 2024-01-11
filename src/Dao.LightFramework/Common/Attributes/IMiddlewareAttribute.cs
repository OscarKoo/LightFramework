using Microsoft.AspNetCore.Http;

namespace Dao.LightFramework.Common.Attributes;

public interface IMiddlewareAttribute
{
    /// <summary>
    /// if (httpContext.Response.HasStarted)
    ///     return;
    /// </summary>
    Task OnExecutionAsync(HttpContext httpContext, IServiceProvider serviceProvider, RequestDelegate next);
}