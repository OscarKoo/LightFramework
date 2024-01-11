using Microsoft.AspNetCore.Mvc.Filters;

namespace Dao.LightFramework.Common.Attributes;

public interface IAsyncActionFilterAttribute
{
    /// <summary>
    /// if (context.Result != null)
    ///     return;
    /// </summary>
    Task<ActionExecutedContext> OnActionExecutionAsync(ActionExecutingContext context, IServiceProvider serviceProvider, ActionExecutionDelegate next);
}