using System.Text;
using Dao.LightFramework.Common.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Dao.LightFramework.HttpApi.Filters;

public class AsyncHandlerFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var sb = new StringBuilder();
        try
        {
            var request = context.HttpContext.Request;
            sb.AppendLine($"Request: {request.Scheme}://{request.Host}{request.Path}{request.QueryString.Value}");
            sb.AppendLine("Parameter: " + context.ActionArguments.ToJson());
            var sw = new StopWatch();
            sw.Start();
            var result = await next();
            var end = sw.Stop();
            if (result.Result is ObjectResult obj)
                sb.AppendLine($"Result: {obj.Value.ToJson()}");
            sb.AppendLine($"Response: Cost {end}");
        }
        finally
        {
            if (sb.Length > 0)
                StaticLogger.LogInformation(sb.ToString());
        }
    }
}