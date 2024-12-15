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
        MicroServiceContext.CreateScopedCache(false);

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

        var httpContext = context.HttpContext;
        var request = httpContext.Request;

        RequestContextInfo.Method = request.Method;
        RequestContextInfo.Route = context.ActionDescriptor.AttributeRouteInfo?.Template;
        var noLog = context.GetRequestParameter(RequestContextInfo.NoLog_Header, RequestContextInfo.NoLog_Query, false).FirstOrDefault().ToInt32();
        RequestContextInfo.NoLog = noLog;
        var logEnabled = RequestContextInfo.IsLogEnabled(noLog);

        var sb = logEnabled ? new StringBuilder() : null;
        string traceId = null;
        try
        {
            TraceContext.TraceId.Renew(request);
            traceId = TraceContext.TraceId.Value;
            TraceContext.SpanId.Renew(request, 1).Degrade();
            TraceContext.ClientId.Renew(request);
            StaticLogger.LogInformation($"TraceId: {traceId}, ConnectionId: {httpContext.Connection.Id} Begin:");

            sb?.AppendLine($"({traceId}, {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}) Request: {request.Method} {request.Scheme}://{request.Host}{request.Path}{request.QueryString.Value}");
            var rc = new RequestContext(this.serviceProvider.GetService<IHttpContextAccessor>());
            //RequestContextInfo.Context = rc;
            if (!string.IsNullOrWhiteSpace(rc.Token))
                rc.Token = "[Token]";
            sb?.AppendLine("RequestContext: " + rc.ToJson());
            sb?.AppendLine("Parameter: " + context.ActionArguments.ToJson());

            var sw = logEnabled ? new StopWatch() : null;
            sw?.Start();

            if (context.ActionDescriptor is ControllerActionDescriptor controllerAction)
            {
                var info = TraceContext.Info.Renew();
                info.ClassName = controllerAction.ControllerName;
                info.MethodName = controllerAction.ActionName;

                foreach (var filter in EnumerateFilters(controllerAction.MethodInfo, controllerAction.ControllerTypeInfo))
                {
                    nextFunc = BuildNext(filter, context, this.serviceProvider, nextFunc);
                    hasFilter = true;
                }
            }

            var result = await nextFunc();
            sw?.Stop();

            if (result.Result is ObjectResult obj)
                sb?.AppendLine($"Result: {obj.Value.ToJson()}");
            if (hasFilter)
                sb?.AppendLine($"Filters: Cost {sw.Format(sw.TotalNS - nextCost)}");
            sb?.Append($"Elapsed: Cost {sw.Format(nextCost)}");
        }
        finally
        {
            if (sb?.Length > 0)
                StaticLogger.LogInformation(sb.ToString());
            StaticLogger.LogInformation($"TraceId: {traceId}, ConnectionId: {httpContext.Connection.Id} End.");
        }
    }

    static readonly ConcurrentDictionary<Tuple<MemberInfo, MemberInfo>, Lazy<IAsyncActionFilterAttribute[]>> filters = new();

    static IEnumerable<IAsyncActionFilterAttribute> EnumerateFilters(MemberInfo action, MemberInfo controller)
    {
        var key = new Tuple<MemberInfo, MemberInfo>(action, controller);
        return filters.GetOrAdd(key, k => new Lazy<IAsyncActionFilterAttribute[]>(() =>
        {
            var actionFilters = GetFilters(k.Item1);
            var controllerFilters = GetFilters(k.Item2);

            foreach (var kv in controllerFilters.Where(kv => !actionFilters.ContainsKey(kv.Key)))
            {
                actionFilters.Add(kv.Key, kv.Value);
            }

            return actionFilters.Where(w => !w.Value.Any(a => a.Disabled)).SelectMany(sm => sm.Value).ToArray();
        })).Value;
    }

    static Dictionary<Type, IAsyncActionFilterAttribute[]> GetFilters(MemberInfo memberInfo) => memberInfo.GetCustomAttributes(true).OfType<IAsyncActionFilterAttribute>().Select(s => new
    {
        Type = s.GetType(),
        Filter = s
    }).GroupBy(g => g.Type).ToDictionary(k => k.Key, v => v.Select(s => s.Filter).ToArray());

    static ActionExecutionDelegate BuildNext(IAsyncActionFilterAttribute filter, ActionExecutingContext context, IServiceProvider serviceProvider, ActionExecutionDelegate next) =>
        async () => await filter.OnActionExecutionAsync(context, serviceProvider, next);
}