using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using Dao.LightFramework.Common.Attributes;
using Dao.LightFramework.Common.Utilities;
using Dao.LightFramework.Traces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Dao.LightFramework.HttpApi.Filters;

public class AsyncHandlerFilter : IAsyncActionFilter
{
    readonly IServiceProvider serviceProvider;

    public AsyncHandlerFilter(IServiceProvider serviceProvider) => this.serviceProvider = serviceProvider;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var sb = new StringBuilder();
        try
        {
            var request = context.HttpContext.Request;
            sb.AppendLine($"({DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}) Request: {request.Method} {request.Scheme}://{request.Host}{request.Path}{request.QueryString.Value}");
            sb.AppendLine("Parameter: " + context.ActionArguments.ToJson());

            var controllerAction = context.ActionDescriptor as ControllerActionDescriptor;
            var filters = new List<FilterState>();
            var sw = new StopWatch();
            if (controllerAction != null)
            {
                var info = TraceContext.Info.Renew();
                info.ClassName = controllerAction.ControllerName;
                info.MethodName = controllerAction.ActionName;
                TraceContext.TraceId.Renew(request);
                TraceContext.SpanId.Renew(request, 1).Degrade();

                sw.Start();
                filters.AddRange(GetFilterStates(controllerAction.MethodInfo));
                filters.AddRange(GetFilterStates(controllerAction.ControllerTypeInfo));

                foreach (var filter in filters)
                {
                    filter.State = await filter.Filter.OnActionExecutingAsync(context, this.serviceProvider);
                    if (context.Result != null)
                        return;
                }

                sw.Stop();
            }

            sw.Start();
            var result = await next();
            var end = sw.Stop();
            var exec = sw.LastStopNS;

            if (result.Result is ObjectResult obj)
                sb.AppendLine($"Result: {obj.Value.ToJson()}");
            sb.AppendLine($"Response: Cost {end}");

            if (controllerAction != null)
            {
                sw.Start();
                foreach (var filter in ((IList<FilterState>)filters).Reverse())
                {
                    await filter.Filter.OnActionExecutedAsync(context, this.serviceProvider, result, filter.State);
                }

                sw.Stop();
                sb.AppendLine($"Filters: Cost {sw.Format(sw.TotalNS - exec)}");
            }
        }
        finally
        {
            if (sb.Length > 0)
                StaticLogger.LogInformation(sb.ToString());
        }
    }

    static readonly ConcurrentDictionary<MemberInfo, Lazy<IAsyncActionFilterAttribute[]>> attributes = new();

    static IEnumerable<FilterState> GetFilterStates(MemberInfo element)
    {
        var attrs = attributes.GetOrAdd(element, k => new Lazy<IAsyncActionFilterAttribute[]>(() => k.GetCustomAttributes(true).OfType<IAsyncActionFilterAttribute>().ToArray())).Value;
        return attrs.Select(s => new FilterState(s));
    }
}

sealed class FilterState
{
    internal FilterState(IAsyncActionFilterAttribute filter) => Filter = filter;

    internal IAsyncActionFilterAttribute Filter { get; set; }
    internal object State { get; set; }
}