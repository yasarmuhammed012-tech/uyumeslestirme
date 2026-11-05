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
    public EslesmeServisi Eslesme { get; }

    public Servisler(UygulamaDbContext db, TimeProvider saat)
    {
        var kullanicilar = new KullaniciDeposu(db);
        var anket = new AnketDeposu(db);
        var eslesmeler = new EslesmeDeposu(db);
        var uyum = new UyumServisi(anket, new UyumSkoruDeposu(db), saat);

        Oturum = new OturumYoneticisi(saat);
        Kimlik = new KimlikServisi(kullanicilar, Oturum, saat);
        Yonetim = new YonetimServisi(kullanicilar, Oturum);
        Anket = new AnketServisi(anket, Oturum);
        Profil = new ProfilServisi(new Depo<Profil>(db), Oturum, saat);
        Adaylar = new AdayServisi(kullanicilar, eslesmeler, uyum, Oturum, saat);
        Eslesme = new EslesmeServisi(eslesmeler, kullanicilar, uyum, Oturum, saat);
    }
}
