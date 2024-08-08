namespace Dao.LightFramework.EntityFrameworkCore.DataProviders;

[Flags]
public enum IgnoreRowVersionMode
{
    None = 0,
    Never = 1,
    Once = 2,
    Scoped = 4
}

public class ScopedIgnoreRowVersionOnSaving : IDisposable
{
    readonly IgnoreRowVersionMode mode;

    ScopedIgnoreRowVersionOnSaving(IgnoreRowVersionMode mode)
    {
        this.mode = mode;
        DbContextCurrent.Add(mode);
    }

    public void Dispose() => DbContextCurrent.Remove(this.mode);

    public static ScopedIgnoreRowVersionOnSaving Create(IgnoreRowVersionMode mode, bool shouldCreate = true) =>
        shouldCreate ? new ScopedIgnoreRowVersionOnSaving(mode) : null;
}