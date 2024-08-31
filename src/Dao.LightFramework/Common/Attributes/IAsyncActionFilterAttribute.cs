using Microsoft.AspNetCore.Mvc.Filters;

namespace Dao.LightFramework.Common.Attributes;

public interface IAsyncActionFilterAttribute
{
    bool Disabled { get; set; }
    /// <summary>
    /// if (context.Result != null)
    ///     return;
    /// </summary>
    Task<ActionExecutedContext> OnActionExecutionAsync(ActionExecutingContext context, IServiceProvider serviceProvider, ActionExecutionDelegate next);
}