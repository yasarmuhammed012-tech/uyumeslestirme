using UyumEslestirme.Data;
using UyumEslestirme.Domain;

namespace UyumEslestirme.Business;

// Arayuzlerin (masaustu ve web) kullandigi servisleri tek yerde olusturur.
public sealed class Servisler
{
    public OturumYoneticisi Oturum { get; }
    public KimlikServisi Kimlik { get; }
    public YonetimServisi Yonetim { get; }
    public AnketServisi Anket { get; }
    public ProfilServisi Profil { get; }
    public AdayServisi Adaylar { get; }
    public EslesmeServisi Eslesme { get; }
    public BildirimServisi Bildirim { get; }
    public MesajServisi Mesaj { get; }
    public OneriServisi Oneri { get; }

    // kuyruk: telefona bildirim gonderen harici kanal (yalnizca API'de). yapayZeka: aciklamali oneri icin
    // dil modeli (yalnizca API'de, anahtar sunucuda). Ikisi de yoksa uygulama yine calisir.
    public Servisler(UygulamaDbContext db, TimeProvider saat, IBildirimKuyrugu? kuyruk = null, IYapayZekaIstemcisi? yapayZeka = null)
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
        Bildirim = new BildirimServisi(new BildirimDeposu(db), Oturum, saat, kuyruk);
        Adaylar = new AdayServisi(kullanicilar, eslesmeler, uyum, Oturum, saat);
        Eslesme = new EslesmeServisi(eslesmeler, kullanicilar, uyum, Bildirim, Oturum, saat);
        Mesaj = new MesajServisi(new MesajDeposu(db), eslesmeler, Bildirim, Oturum, saat);
        Oneri = new OneriServisi(Adaylar, uyum, Oturum, yapayZeka);
    }
}
