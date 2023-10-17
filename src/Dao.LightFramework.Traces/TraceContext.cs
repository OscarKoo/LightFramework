using Microsoft.AspNetCore.Http;

namespace Dao.LightFramework.Traces;

public static class TraceContext
{
    public static string ProductName { get; set; }
    public static string ServiceName { get; set; }

    #region AsyncLocal

    static readonly AsyncLocal<LocalContext> asyncLocalContext = new();
    public static LocalContext AsyncContext => asyncLocalContext.Value ??= new LocalContext();

    #endregion

    #region ThreadLocal

    //static readonly ThreadLocal<LocalContext> threadLocalContext = new();
    //public static LocalContext ThreadContext => threadLocalContext.Value ??= new LocalContext();

    #endregion

    public static TraceId TraceId => AsyncContext.TraceId ??= new TraceId();
    public static SpanId SpanId => AsyncContext.SpanId ??= new SpanId();

    public static void ResetIds(HttpRequest request = null, int spanIdSeed = 0)
    {
        TraceId.Reset(request);
        SpanId.Reset(request, spanIdSeed);
    }

    internal static int ToInt32(this string source, int defaultValue = 0) =>
        string.IsNullOrWhiteSpace(source) || !int.TryParse(source, out var number)
            ? defaultValue
            : number;
}