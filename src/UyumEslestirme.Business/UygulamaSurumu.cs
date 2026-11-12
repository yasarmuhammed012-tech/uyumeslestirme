using System.Reflection;

namespace UyumEslestirme.Business;

// Surum numarasi Directory.Build.props icindeki <Version> alanindan gelir (semantic versioning).
// Derleyicinin ekledigi "+commit" son eki ekranda gosterilmez.
public static class UygulamaSurumu
{
    public static string Metin { get; } = "v" + (typeof(UygulamaSurumu).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0")
        .Split('+')[0];
}
