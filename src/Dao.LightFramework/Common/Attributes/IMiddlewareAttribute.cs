using Microsoft.AspNetCore.Http;

namespace Dao.LightFramework.Common.Attributes;

public interface IMiddlewareAttribute
{
    Task<object> OnExecutingAsync(HttpContext httpContext);
    Task OnExecutedAsync(HttpContext httpContext, object state);
}