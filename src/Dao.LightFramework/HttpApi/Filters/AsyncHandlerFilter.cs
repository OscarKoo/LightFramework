using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using Dao.LightFramework.Common.Attributes;
using Dao.LightFramework.Common.Utilities;
using Dao.LightFramework.Services.Contexts;
using Dao.LightFramework.Traces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Dao.LightFramework.HttpApi.Filters;

public class AsyncHandlerFilter : IAsyncActionFilter
{
    readonly IServiceProvider serviceProvider;

    public AsyncHandlerFilter(IServiceProvider serviceProvider) => this.serviceProvider = serviceProvider;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var hasFilter = false;
        double nextCost = 0;
        ActionExecutionDelegate nextFunc = async () =>
        {
            var exec = new StopWatch();
            exec.Start();
            var result = await next();
            exec.Stop();
            nextCost = exec.LastStopNS;
            return result;
        };

        var sb = new StringBuilder();
        var httpContext = context.HttpContext;
        try
        {
            var request = httpContext.Request;
            TraceContext.TraceId.Renew(request);
            TraceContext.SpanId.Renew(request, 1).Degrade();
            TraceContext.ClientId.Renew(request);
            StaticLogger.LogInformation($"TraceId: {TraceContext.TraceId.Value}, ConnectionId: {httpContext.Connection.Id} Begin:");

            sb.AppendLine($"({DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}) Request: {request.Method} {request.Scheme}://{request.Host}{request.Path}{request.QueryString.Value}");
            sb.AppendLine("RequestContext: " + new RequestContext(this.serviceProvider.GetService<IHttpContextAccessor>()).ToJson());
            sb.AppendLine("Parameter: " + context.ActionArguments.ToJson());

            var sw = new StopWatch();
            sw.Start();

            if (context.ActionDescriptor is ControllerActionDescriptor controllerAction)
            {
                var info = TraceContext.Info.Renew();
                info.ClassName = controllerAction.ControllerName;
                info.MethodName = controllerAction.ActionName;

                foreach (var filter in GetFilters(controllerAction.MethodInfo).Concat(GetFilters(controllerAction.ControllerTypeInfo)))
                {
                    nextFunc = BuildNext(filter, context, this.serviceProvider, nextFunc);
                    hasFilter = true;
                }
            }

            var result = await nextFunc();
            sw.Stop();

            if (result.Result is ObjectResult obj)
                sb.AppendLine($"Result: {obj.Value.ToJson()}");
            if (hasFilter)
                sb.AppendLine($"Filters: Cost {sw.Format(sw.TotalNS - nextCost)}");
            sb.AppendLine($"Elapsed: Cost {sw.Format(nextCost)}");
        }
        finally
        {
            if (sb.Length > 0)
                StaticLogger.LogInformation(sb.ToString());
            StaticLogger.LogInformation($"TraceId: {TraceContext.TraceId.Value}, ConnectionId: {httpContext.Connection.Id} End.");
        }
    }

    static readonly ConcurrentDictionary<MemberInfo, Lazy<IAsyncActionFilterAttribute[]>> filters = new();

    static IEnumerable<IAsyncActionFilterAttribute> GetFilters(MemberInfo element) =>
        filters.GetOrAdd(element, k => new Lazy<IAsyncActionFilterAttribute[]>(() => k.GetCustomAttributes(true).OfType<IAsyncActionFilterAttribute>().ToArray())).Value;

    static ActionExecutionDelegate BuildNext(IAsyncActionFilterAttribute filter, ActionExecutingContext context, IServiceProvider serviceProvider, ActionExecutionDelegate next) =>
        async () => await filter.OnActionExecutionAsync(context, serviceProvider, next);
}