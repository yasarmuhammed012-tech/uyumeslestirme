using Microsoft.EntityFrameworkCore;
using UyumEslestirme.Business;
using UyumEslestirme.Data;
using UyumEslestirme.Domain;

namespace UyumEslestirme.Tests;

public enum Saglayici
{
    Sqlite,
    Bellek,
}

// Entegrasyon testleri: modulleri tek tek degil, arayuzlerin kullandigi Servisler sinifi uzerinden
// birlikte calistirir. Her senaryo iki veritabaninda kosar: masaustunun SQLite'i ve web surumunun
// bellek ici veritabani. Boylece bir saglayicida calisip digerinde bozulan kod (regresyon) yakalanir.
public sealed class UctanUcaTestleri
{
    private const string Parola = "Parola123";

    private readonly SahteSaat _saat = new();

    [Theory]
    [InlineData(Saglayici.Sqlite)]
    [InlineData(Saglayici.Bellek)]
    public void Kayittan_eslesmeye_tum_moduller_birlikte_calisir(Saglayici saglayici)
    {
        using var db = Veritabani(saglayici);
        var s = new Servisler(db.Db, _saat);

        // Hafta 5: kayit. Hafta 6: profil ve anket.
        s.Kimlik.KayitOl("Ayşe Deneme", "ayse@ornek.test", Parola, Parola);
        s.Kimlik.KayitOl("Mert Deneme", "mert@ornek.test", Parola, Parola);
        ProfilVeAnket(s, "ayse@ornek.test", "Bingöl", deger: 4);
        ProfilVeAnket(s, "mert@ornek.test", "Bingöl", deger: 3);

        // Hafta 7: aday listesi ve istek.
        s.Kimlik.GirisYap("ayse@ornek.test", Parola);
        var aday = Assert.Single(s.Adaylar.Adaylar());
        Assert.Equal("Mert Deneme", aday.AdSoyad);
        Assert.Equal(AdayDurumu.Yok, aday.Durum);
        Assert.Equal(75, aday.UyumYuzdesi, 6);
        s.Eslesme.IstekGonder(aday.KullaniciId);
        Assert.Equal(AdayDurumu.IstekGonderildi, Assert.Single(s.Adaylar.Adaylar()).Durum);
        s.Kimlik.CikisYap();

        // Karsi taraf istegi gorur; kabulden once e-posta gizlidir.
        s.Kimlik.GirisYap("mert@ornek.test", Parola);
        var gelen = Assert.Single(s.Eslesme.Listele().GelenIstekler);
        Assert.Equal("Ayşe Deneme", gelen.AdSoyad);
        Assert.Null(gelen.Eposta);
        s.Eslesme.Yanitla(gelen.EslesmeId, kabul: true);
        Assert.Equal("ayse@ornek.test", Assert.Single(s.Eslesme.Listele().Eslesmeler).Eposta);
        s.Kimlik.CikisYap();

        // Gonderen taraf da eslesmeyi ve iletisim bilgisini gorur.
        s.Kimlik.GirisYap("ayse@ornek.test", Parola);
        Assert.Equal(AdayDurumu.Eslesti, Assert.Single(s.Adaylar.Adaylar()).Durum);
        Assert.Equal("mert@ornek.test", Assert.Single(s.Eslesme.Listele().Eslesmeler).Eposta);
    }

    // Anket (Hafta 6) degisince onbellekteki skor (Hafta 7) silinmeli ve her ekranda yeni skor gorunmeli.
    // Web surumu eklenirken bu islem bellek ici veritabaninda calismiyordu; test bu hatanin geri gelmesini engeller.
    [Theory]
    [InlineData(Saglayici.Sqlite)]
    [InlineData(Saglayici.Bellek)]
    public void Anket_degisince_skor_tum_ekranlarda_yenilenir(Saglayici saglayici)
    {
        using var db = Veritabani(saglayici);
        var s = new Servisler(db.Db, _saat);
        s.Kimlik.KayitOl("Ayşe Deneme", "ayse@ornek.test", Parola, Parola);
        s.Kimlik.KayitOl("Mert Deneme", "mert@ornek.test", Parola, Parola);
        ProfilVeAnket(s, "ayse@ornek.test", "Bingöl", deger: 3);
        ProfilVeAnket(s, "mert@ornek.test", "Bingöl", deger: 3);

        s.Kimlik.GirisYap("ayse@ornek.test", Parola);
        Assert.Equal(100, Assert.Single(s.Adaylar.Adaylar()).UyumYuzdesi, 6);
        s.Eslesme.IstekGonder(Assert.Single(s.Adaylar.Adaylar()).KullaniciId);
        s.Kimlik.CikisYap();

        // Mert anketini iki kez degistirir; ikinci kayit da eski cevaplarin uzerine yazilmalidir.
        ProfilVeAnket(s, "mert@ornek.test", "Bingöl", deger: 1);
        ProfilVeAnket(s, "mert@ornek.test", "Bingöl", deger: 5);

        s.Kimlik.GirisYap("ayse@ornek.test", Parola);
        Assert.Equal(50, Assert.Single(s.Adaylar.Adaylar()).UyumYuzdesi, 6);
        Assert.Equal(50, Assert.Single(s.Eslesme.Listele().GonderilenIstekler).UyumYuzdesi!.Value, 6);
        Assert.Equal(1, db.Db.UyumSkorlari.Count());
    }

    // Demo senaryosu (docs/hafta-8-ara-surum.md) ornek veriye dayanir; veri degisirse senaryo bozulmasin.
    [Theory]
    [InlineData(Saglayici.Sqlite)]
    [InlineData(Saglayici.Bellek)]
    public void Ornek_veri_demo_senaryosunu_karsilar(Saglayici saglayici)
    {
        using var db = Veritabani(saglayici);
        var hash = ParolaHasleyici.Hashle("Demo1234");
        VeriTohumlayici.Tohumla(db.Db, hash, hash, _saat.GetUtcNow().UtcDateTime);
        var s = new Servisler(db.Db, _saat);

        s.Kimlik.GirisYap("ogrenci01@ornek.test", "Demo1234");
        var listeler = s.Eslesme.Listele();
        Assert.Equal("Örnek Öğrenci 02", Assert.Single(listeler.GelenIstekler).AdSoyad);
        Assert.Equal("ogrenci03@ornek.test", Assert.Single(listeler.Eslesmeler).Eposta);
        Assert.Empty(listeler.GonderilenIstekler);

        var adaylar = s.Adaylar.Adaylar();
        Assert.Equal(19, adaylar.Count);
        Assert.Contains(adaylar, a => a.Durum == AdayDurumu.Yok);
        s.Kimlik.CikisYap();

        s.Kimlik.GirisYap("yonetici@ornek.test", "Demo1234");
        Assert.Equal(21, s.Yonetim.KullanicilariListele().Count);
        Assert.Throws<YetkisizErisimHatasi>(() => s.Eslesme.IstekGonder(adaylar[0].KullaniciId));
    }

    private void ProfilVeAnket(Servisler s, string eposta, string sehir, int deger)
    {
        s.Kimlik.GirisYap(eposta, Parola);
        s.Profil.Kaydet(new ProfilGirdisi("Bilgisayar Programcılığı", sehir, _saat.GetUtcNow().Year - 20, null));
        s.Anket.CevaplariKaydet(s.Anket.Sorular().Select(soru => new CevapGirdisi(soru.Id, deger, 3)).ToList());
        s.Kimlik.CikisYap();
    }

    private static TestVeriKaynagi Veritabani(Saglayici saglayici)
    {
        if (saglayici == Saglayici.Sqlite)
        {
            var sqlite = new TestVeritabani();
            VeriTohumlayici.SorulariTohumla(sqlite.Db);
            return new TestVeriKaynagi(sqlite.Db, sqlite);
        }

        // Web surumundeki (src/UyumEslestirme.Web/Program.cs) ayarin aynisi.
        var bellek = new UygulamaDbContext(new DbContextOptionsBuilder<UygulamaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        VeriTohumlayici.SorulariTohumla(bellek);
        return new TestVeriKaynagi(bellek, bellek);
    }

    private sealed class TestVeriKaynagi(UygulamaDbContext db, IDisposable sahip) : IDisposable
    {
        public UygulamaDbContext Db { get; } = db;

        public void Dispose() => sahip.Dispose();
    }
}
