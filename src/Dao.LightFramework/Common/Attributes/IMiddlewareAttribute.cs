using Microsoft.AspNetCore.Http;

namespace Dao.LightFramework.Common.Attributes;

public interface IMiddlewareAttribute
{
    Task<object> OnExecutingAsync(HttpContext httpContext, IServiceProvider serviceProvider);
    Task OnExecutedAsync(HttpContext httpContext, IServiceProvider serviceProvider, object state);
}