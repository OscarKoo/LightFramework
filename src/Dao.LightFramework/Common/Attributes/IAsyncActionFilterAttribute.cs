using Microsoft.AspNetCore.Mvc.Filters;

namespace Dao.LightFramework.Common.Attributes;

public interface IAsyncActionFilterAttribute
{
    Task<ActionExecutedContext> OnActionExecutionAsync(ActionExecutingContext context, IServiceProvider serviceProvider, ActionExecutionDelegate next);
}