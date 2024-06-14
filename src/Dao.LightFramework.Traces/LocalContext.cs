using MassTransit;
using Microsoft.AspNetCore.Http;

namespace Dao.LightFramework.Traces;

public abstract class LocalContext<T> where T : LocalContext<T>, new()
{
    protected T Renew(AsyncLocal<T> host, Action<T> onCreating = null)
    {
        var context = new T();
        onCreating?.Invoke(context);
        host.Value = context;
        return context;
    }
}

public class ContextInfo : LocalContext<ContextInfo>
{
    public string ModuleName { get; set; }
    public string ClassName { get; set; }
    public string MethodName { get; set; }

    public ContextInfo Renew() => Renew(TraceContext.AsyncContextInfo);
}

public class TraceId : LocalContext<TraceId>
{
    public const string Header = "X-Trace-Id";

    public string Value { get; set; }

    public TraceId Renew(HttpRequest request = null, string defaultValue = null) => Renew(TraceContext.AsyncTraceId,
        ctx => ctx.Value = request?.Headers[Header].FirstOrDefault(w => !string.IsNullOrWhiteSpace(w))
            ?? defaultValue
            ?? TraceContext.TraceId.Value
            ?? NewId.NextSequentialGuid().ToString());
}

public class SpanId : LocalContext<SpanId>
{
    public const string Header = "X-Span-Id";

    string prefix;
    int seed;

    public string Value => !string.IsNullOrWhiteSpace(this.prefix)
        ? $"{this.prefix}.{this.seed}"
        : this.seed > 0
            ? $"{this.seed}"
            : "0";

    public bool HasValue => !string.IsNullOrWhiteSpace(this.prefix) || this.seed > 0;

    public SpanId Renew(HttpRequest request = null, int defaultSeed = 0) => Renew(TraceContext.AsyncSpanId, ctx =>
    {
        var value = request?.Headers[Header].FirstOrDefault(w => !string.IsNullOrWhiteSpace(w));

        if (string.IsNullOrWhiteSpace(value))
        {
            ctx.prefix = null;
            ctx.seed = defaultSeed;
        }
        else
        {
            var digital = value.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            ctx.prefix = digital.Length == 1 ? null : string.Join(".", digital[..^1]);
            ctx.seed = digital[^1].ToInt32();
        }
    });

    public SpanId Continue()
    {
        lock (this)
        {
            this.seed++;
            return this;
        }
    }

    public SpanId ContinueRenew()
    {
        lock (this)
        {
            this.seed++;
            return Renew(TraceContext.AsyncSpanId, ctx =>
            {
                ctx.prefix = this.prefix;
                ctx.seed = this.seed;
            });
        }
    }

    #region Degrade

    public SpanId Degrade(bool force = false) => Renew(TraceContext.AsyncSpanId, ctx =>
    {
        if (force || HasValue)
            ctx.prefix = Value;
    });

    //bool isDegrading;

    //public IDisposable Degrading()
    //{
    //    if (this.isDegrading)
    //        return null;

    //    lock (this)
    //    {
    //        if (this.isDegrading)
    //            return null;

    //        this.isDegrading = true;

    //        this.prefix = Value;
    //        this.seed = 0;

    //        return new SpanIdDegrading(this);
    //    }
    //}

    //sealed class SpanIdDegrading : IDisposable
    //{
    //    SpanId instance;

    //    internal SpanIdDegrading(SpanId instance) => this.instance = instance;

    //    public void Dispose()
    //    {
    //        var tmp = this.instance;
    //        this.instance = null;
    //        tmp.isDegrading = false;
    //    }
    //}

    #endregion
}

public class ClientId : LocalContext<ClientId>
{
    public const string Header = "X-Client-Id";

    public string Value { get; set; }

    public ClientId Renew(HttpRequest request = null, string defaultValue = null) => Renew(TraceContext.AsyncClientId,
        ctx => ctx.Value = request?.Headers[Header].FirstOrDefault(w => !string.IsNullOrWhiteSpace(w))
            ?? defaultValue
            ?? TraceContext.TraceId.Value);
}