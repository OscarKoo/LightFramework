namespace Dao.LightFramework.Traces;

public static class TraceContext
{
    public static string ProductName { get; set; }
    public static string ServiceName { get; set; }

    #region AsyncLocal

    internal static readonly AsyncLocal<ContextInfo> AsyncContextInfo = new();
    internal static readonly AsyncLocal<TraceId> AsyncTraceId = new();
    internal static readonly AsyncLocal<SpanId> AsyncSpanId = new();
    internal static readonly AsyncLocal<ClientId> AsyncClientId = new();

    #endregion

    #region ThreadLocal

    //static readonly ThreadLocal<LocalContext> threadLocalContext = new();
    //public static LocalContext ThreadContext => threadLocalContext.Value ??= new LocalContext();

    #endregion

    public static ContextInfo Info => AsyncContextInfo.Value ??= new ContextInfo();
    public static TraceId TraceId => AsyncTraceId.Value ??= new TraceId();
    public static SpanId SpanId => AsyncSpanId.Value ??= new SpanId();
    public static ClientId ClientId => AsyncClientId.Value ??= new ClientId();

    internal static int ToInt32(this string source, int defaultValue = 0) =>
        string.IsNullOrWhiteSpace(source) || !int.TryParse(source, out var number)
            ? defaultValue
            : number;
}