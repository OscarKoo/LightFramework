using System.Diagnostics;

namespace Dao.LightFramework.Common.Utilities;

public class StopWatch
{
    readonly Stopwatch sw = new();
    readonly uint attention;
    double total;

    public StopWatch(uint attention = 1000) => this.attention = attention;

    public void Start() => this.sw.Restart();

    public string Stop()
    {
        this.sw.Stop();
        var ns = 1000000000 * (double)this.sw.ElapsedTicks / Stopwatch.Frequency;
        this.total += ns;
        return Format(ns);
    }

    public string Total => Format(this.total);

    string Format(double elapsed)
    {
        var ms = Math.Round(elapsed / 1000000, 1);
        return $"{(ms > this.attention ? "[ATTENTION] " : "")}{ms} ms, {Math.Round(elapsed / 1000, 1)} us, {Math.Round(elapsed, 1)} ns";
    }
}