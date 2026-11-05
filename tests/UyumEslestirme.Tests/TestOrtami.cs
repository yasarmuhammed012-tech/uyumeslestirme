using UyumEslestirme.Business;
using UyumEslestirme.Data;
using UyumEslestirme.Domain;

namespace UyumEslestirme.Tests;

// Uyum ve eslestirme testleri icin servislerin hazir kuruldugu ortam.
public sealed class TestOrtami : IDisposable
{
    private readonly TestVeritabani _t = new();

    public SahteSaat Saat { get; } = new();
    public OturumYoneticisi Oturum { get; }
    public AnketDeposu AnketDeposu { get; }
    public AnketServisi Anket { get; }
    public KullaniciDeposu Kullanicilar { get; }
    public ProfilServisi Profil { get; }
    public AdayServisi Adaylar { get; }
    public EslesmeServisi Eslesme { get; }

    public UygulamaDbContext Db => _t.Db;

    public TestOrtami()
    {
        Oturum = new OturumYoneticisi(Saat);
        AnketDeposu = new AnketDeposu(Db);
        Anket = new AnketServisi(AnketDeposu, Oturum);
        Kullanicilar = new KullaniciDeposu(Db);
        Profil = new ProfilServisi(new Depo<Profil>(Db), Oturum, Saat);
        var eslesmeler = new EslesmeDeposu(Db);
        var uyum = new UyumServisi(AnketDeposu, new UyumSkoruDeposu(Db), Saat);
        Adaylar = new AdayServisi(Kullanicilar, eslesmeler, uyum, Oturum, Saat);
        Eslesme = new EslesmeServisi(eslesmeler, Kullanicilar, uyum, Oturum, Saat);
        VeriTohumlayici.SorulariTohumla(Db);
    }

    public void Dispose() => _t.Dispose();

    public int Ogrenci(
        string ad,
        bool profil = true,
        Func<AnketSorusu, (int Deger, int Agirlik)>? cevap = null,
        Rol rol = Rol.Ogrenci,
        bool gorunur = true)
    {
        var kullanici = Kullanicilar.Ekle(new Kullanici
        {
            Eposta = $"{ad}@ornek.test",
            AdSoyad = ad,
            ParolaHash = VeriTohumlayici.GirisKapaliHash,
            Rol = rol,
            OlusturmaTarihi = DateTime.UtcNow,
            Profil = profil ? new Profil { Bolum = "Muhasebe", Sehir = "Bingöl", DogumYili = 2005, Gorunur = gorunur } : null,
        });
        if (cevap is not null)
        {
            Oturum.Ac(kullanici.Id, ad, rol);
            CevaplariKaydet(cevap);
        }
        Oturum.Kapat();
        return kullanici.Id;
    }

    // Tum sorulari, ayni cevaplari verecek sekilde 3/3 ile dolduran kisayol.
    public int TamOgrenci(string ad, bool gorunur = true) => Ogrenci(ad, cevap: _ => (3, 3), gorunur: gorunur);

    public void CevaplariKaydet(Func<AnketSorusu, (int Deger, int Agirlik)> cevap) =>
        Anket.CevaplariKaydet(AnketDeposu.AktifSorular().Select(s =>
        {
            var (deger, agirlik) = cevap(s);
            return new CevapGirdisi(s.Id, deger, agirlik);
        }).ToList());

    public void GirisYap(int id, Rol rol = Rol.Ogrenci) => Oturum.Ac(id, "Test", rol);
}
