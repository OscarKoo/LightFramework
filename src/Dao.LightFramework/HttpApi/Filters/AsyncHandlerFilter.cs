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

            double nextCost = 0;
            ActionExecutionDelegate nextFunc = async () =>
            {
                var sw = new StopWatch();
                sw.Start();
                var result = await next();
                sw.Stop();
                nextCost = sw.LastStopNS;
                return result;
            };

            var sw = new StopWatch();
            sw.Start();

            var controllerAction = context.ActionDescriptor as ControllerActionDescriptor;
            if (controllerAction != null)
            {
                var info = TraceContext.Info.Renew();
                info.ClassName = controllerAction.ControllerName;
                info.MethodName = controllerAction.ActionName;
                TraceContext.TraceId.Renew(request);
                TraceContext.SpanId.Renew(request, 1).Degrade();

                nextFunc = GetFilters(controllerAction.MethodInfo).Concat(GetFilters(controllerAction.ControllerTypeInfo)).Aggregate(nextFunc, (current, filter) => BuildNext(filter, context, this.serviceProvider, current));
            }

            var result = await nextFunc();
            sw.Stop();

            if (result.Result is ObjectResult obj)
                sb.AppendLine($"Result: {obj.Value.ToJson()}");
            sb.AppendLine($"Response: Cost {nextCost}");

            if (controllerAction != null)
            {
                sb.AppendLine($"Filters: Cost {sw.Format(sw.TotalNS - nextCost)}");
            }
        }
        finally
        {
            if (sb.Length > 0)
                StaticLogger.LogInformation(sb.ToString());
        }
    }

    static readonly ConcurrentDictionary<MemberInfo, Lazy<IAsyncActionFilterAttribute[]>> filters = new();

    static IEnumerable<IAsyncActionFilterAttribute> GetFilters(MemberInfo element) =>
        filters.GetOrAdd(element, k => new Lazy<IAsyncActionFilterAttribute[]>(() => k.GetCustomAttributes(true).OfType<IAsyncActionFilterAttribute>().ToArray())).Value;

    static ActionExecutionDelegate BuildNext(IAsyncActionFilterAttribute filter, ActionExecutingContext context, IServiceProvider serviceProvider, ActionExecutionDelegate next) =>
        async () => await filter.OnActionExecutionAsync(context, serviceProvider, next);
}