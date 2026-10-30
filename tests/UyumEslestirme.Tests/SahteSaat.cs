namespace UyumEslestirme.Tests;

public sealed class SahteSaat : TimeProvider
{
    public DateTimeOffset Simdi { get; set; } = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => Simdi;

    public void Ilerlet(TimeSpan sure) => Simdi += sure;
}
