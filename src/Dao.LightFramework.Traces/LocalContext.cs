using MassTransit;
using Microsoft.AspNetCore.Http;

namespace Dao.LightFramework.Traces;

public class LocalContext
{
    internal TraceId TraceId { get; set; }
    internal SpanId SpanId { get; set; }

    public string ModuleName { get; set; }
    public string ClassName { get; set; }
    public string MethodName { get; set; }
}

public class TraceId
{
    public const string Header = "X-Trace-Id";

    public string Value { get; set; }

    public TraceId Reset(HttpRequest request = null, string defaultValue = null)
    {
        var value = request?.Headers[Header].FirstOrDefault(w => !string.IsNullOrWhiteSpace(w));

        if (string.IsNullOrWhiteSpace(value))
            Value = defaultValue ?? NewId.NextSequentialGuid().ToString();

        return this;
    }
}

public class SpanId
{
    public const string Header = "X-Span-Id";

    int locked;
    string prefix;
    int seed;
    public string Value => !string.IsNullOrWhiteSpace(this.prefix)
        ? $"{this.prefix}.{this.seed}"
        : this.seed > 0
            ? $"{this.seed}"
            : "0";

    public bool HasValue => !string.IsNullOrWhiteSpace(this.prefix) || this.seed > 0;

    public SpanId Reset(HttpRequest request = null, int seed = 0)
    {
        var value = request?.Headers[Header].FirstOrDefault(w => !string.IsNullOrWhiteSpace(w));

        if (string.IsNullOrWhiteSpace(value))
        {
            this.prefix = null;
            this.seed = seed;
        }

        return this;
    }

    public SpanId Continue()
    {
        this.seed++;
        return this;
    }

    public SpanId Degrade()
    {
        if (Interlocked.Add(ref this.locked, 0) == 0)
        {
            this.prefix = Value;
            this.seed = 0;
        }

        return this;
    }

    public SpanId Lock()
    {
        Interlocked.Increment(ref this.locked);
        return this;
    }

    public SpanId Unlock()
    {
        Interlocked.Decrement(ref this.locked);
        return this;
    }
}