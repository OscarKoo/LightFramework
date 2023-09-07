﻿using Microsoft.AspNetCore.Mvc.Filters;

namespace Dao.LightFramework.Common.Attributes;

public interface IActionFilterAttribute
{
    Task<object> OnActionExecutingAsync(ActionExecutingContext executingContext, IServiceProvider serviceProvider);
    Task OnActionExecutedAsync(ActionExecutingContext executingContext, IServiceProvider serviceProvider, ActionExecutedContext executedContext, object state);
}