using System.Diagnostics;

namespace Dao.LightFramework.Common.Utilities;

public class StopWatch
{
    readonly Stopwatch sw = new();
    readonly uint attention;

    public double LastStopNS { get; private set; }
    public double TotalNS { get; private set; }

    public StopWatch(uint attention = 1000) => this.attention = attention;

    public void Start() => this.sw.Restart();

    public string Stop()
    {
        this.sw.Stop();
        LastStopNS = this.sw.ElapsedNanoseconds();
        TotalNS += LastStopNS;
        return Format(LastStopNS);
    }

    public string Total => Format(TotalNS);

    public string Format(double elapsed)
    {
        var ms = this.sw.RoundMilliseconds(elapsed);
        return $"{(ms > this.attention ? "[ATTENTION] " : "")}{ms} ms, {this.sw.RoundMicroseconds(elapsed)} us, {this.sw.RoundNanoseconds(elapsed)} ns";
    }
}

public static class StopwatchExtensions
{
    public static double ElapsedNanoseconds(this Stopwatch sw) => 1000000000 * (double)sw.ElapsedTicks / Stopwatch.Frequency;

    public static double RoundNanoseconds(this Stopwatch sw, double? nanoseconds = null, int digits = 1) => Math.Round(nanoseconds ?? sw.ElapsedNanoseconds(), digits);
    public static double RoundMicroseconds(this Stopwatch sw, double? nanoseconds = null, int digits = 1) => Math.Round(nanoseconds ?? sw.ElapsedNanoseconds() / 1000, digits);
    public static double RoundMilliseconds(this Stopwatch sw, double? nanoseconds = null, int digits = 1) => Math.Round(nanoseconds ?? sw.ElapsedNanoseconds() / 1000000, digits);
}