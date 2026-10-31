using UyumEslestirme.Business;
using UyumEslestirme.Data;
using UyumEslestirme.Domain;

namespace UyumEslestirme.Desktop;

// Formlarin kullandigi servisleri tek yerde olusturur.
public sealed class Servisler
{
    public OturumYoneticisi Oturum { get; }
    public KimlikServisi Kimlik { get; }
    public YonetimServisi Yonetim { get; }
    public AnketServisi Anket { get; }
    public ProfilServisi Profil { get; }
    public AdayServisi Adaylar { get; }

    public Servisler(UygulamaDbContext db, TimeProvider saat)
    {
        var kullanicilar = new KullaniciDeposu(db);
        Oturum = new OturumYoneticisi(saat);
        Kimlik = new KimlikServisi(kullanicilar, Oturum, saat);
        Yonetim = new YonetimServisi(kullanicilar, Oturum);
        var anket = new AnketDeposu(db);
        Anket = new AnketServisi(anket, Oturum);
        Adaylar = new AdayServisi(kullanicilar, anket, Oturum, saat);
        Profil = new ProfilServisi(new Depo<Profil>(db), Oturum, saat);
    }
}
