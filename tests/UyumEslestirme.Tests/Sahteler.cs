using UyumEslestirme.Business;

namespace UyumEslestirme.Tests;

// Harici bildirim kuyrugunun yerine gecer: gonderilecek isleri listede tutar.
public sealed class SahteKuyruk : IBildirimKuyrugu
{
    public List<HariciBildirimIsi> Isler { get; } = [];
    public bool Dolu { get; set; }

    public bool Ekle(HariciBildirimIsi is_)
    {
        if (Dolu)
            return false;
        Isler.Add(is_);
        return true;
    }
}

// Dil modelinin yerine gecer: verilen yaniti dondurur veya hata firlatir; gelen istemi saklar.
public sealed class SahteYapayZeka : IYapayZekaIstemcisi
{
    public Func<string, string>? Yanit { get; set; }
    public Exception? Hata { get; set; }
    public TimeSpan Gecikme { get; set; }
    public string? SonIstem { get; private set; }
    public int CagriSayisi { get; private set; }

    public async Task<string> JsonUret(string sistem, string istem, string jsonSemasi, CancellationToken iptal)
    {
        CagriSayisi++;
        SonIstem = istem;
        if (Gecikme > TimeSpan.Zero)
            await Task.Delay(Gecikme, iptal);
        if (Hata is not null)
            throw Hata;
        return Yanit?.Invoke(istem) ?? "";
    }
}
