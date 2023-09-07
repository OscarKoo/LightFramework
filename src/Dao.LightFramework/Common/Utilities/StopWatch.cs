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
        LastStopNS = 1000000000 * (double)this.sw.ElapsedTicks / Stopwatch.Frequency;
        TotalNS += LastStopNS;
        return Format(LastStopNS);
    }

    public string Total => Format(TotalNS);

    public string Format(double elapsed)
    {
        var ms = Math.Round(elapsed / 1000000, 1);
        return $"{(ms > this.attention ? "[ATTENTION] " : "")}{ms} ms, {Math.Round(elapsed / 1000, 1)} us, {Math.Round(elapsed, 1)} ns";
    }
}