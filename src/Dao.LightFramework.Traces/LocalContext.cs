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
            value = defaultValue ?? NewId.NextSequentialGuid().ToString();

        Value = value;
        return this;
    }
}

public class SpanId
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

    public SpanId Reset(HttpRequest request = null, int defaultSeed = 0)
    {
        var value = request?.Headers[Header].FirstOrDefault(w => !string.IsNullOrWhiteSpace(w));

        if (string.IsNullOrWhiteSpace(value))
        {
            this.prefix = null;
            this.seed = defaultSeed;
        }
        else
        {
            var digital = value.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            this.prefix = digital.Length == 1 ? null : string.Join(".", digital[..^1]);
            this.seed = digital[^1].ToInt32();
        }

        return this;
    }

    public SpanId Continue()
    {
        this.seed++;
        return this;
    }

    #region Degrade

    public SpanId Degrade()
    {
        this.prefix = Value;
        this.seed = 0;
        return this;
    }

    bool isDegrading;

    public IDisposable Degrading()
    {
        if (this.isDegrading)
            return null;

        lock (this)
        {
            if (this.isDegrading)
                return null;

            this.isDegrading = true;

            this.prefix = Value;
            this.seed = 0;

            return new SpanIdDegrading(this);
        }
    }

    sealed class SpanIdDegrading : IDisposable
    {
        SpanId instance;

        internal SpanIdDegrading(SpanId instance) => this.instance = instance;

        public void Dispose()
        {
            var tmp = this.instance;
            this.instance = null;
            tmp.isDegrading = false;
        }
    }

    #endregion
}