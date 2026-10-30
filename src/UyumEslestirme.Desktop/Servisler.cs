using UyumEslestirme.Business;
using UyumEslestirme.Data;

namespace UyumEslestirme.Desktop;

// Formlarin kullandigi servisleri tek yerde olusturur.
public sealed class Servisler
{
    public OturumYoneticisi Oturum { get; }
    public KimlikServisi Kimlik { get; }
    public YonetimServisi Yonetim { get; }

    public Servisler(UygulamaDbContext db, TimeProvider saat)
    {
        var kullanicilar = new KullaniciDeposu(db);
        Oturum = new OturumYoneticisi(saat);
        Kimlik = new KimlikServisi(kullanicilar, Oturum, saat);
        Yonetim = new YonetimServisi(kullanicilar, Oturum);
    }
}
