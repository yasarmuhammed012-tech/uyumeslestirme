using UyumEslestirme.Business;
using UyumEslestirme.Data;
using UyumEslestirme.Domain;

namespace UyumEslestirme.Tests;

public sealed class AdayTestleri : IDisposable
{
    private readonly TestVeritabani _t = new();
    private readonly SahteSaat _saat = new();
    private readonly OturumYoneticisi _oturum;
    private readonly AnketDeposu _anketDeposu;
    private readonly AnketServisi _anket;
    private readonly AdayServisi _adaylar;
    private readonly KullaniciDeposu _kullanicilar;

    public AdayTestleri()
    {
        _oturum = new OturumYoneticisi(_saat);
        _anketDeposu = new AnketDeposu(_t.Db);
        _anket = new AnketServisi(_anketDeposu, _oturum);
        _kullanicilar = new KullaniciDeposu(_t.Db);
        _adaylar = new AdayServisi(_kullanicilar, _anketDeposu, _oturum, _saat);
        VeriTohumlayici.SorulariTohumla(_t.Db);
    }

    public void Dispose() => _t.Dispose();

    private int Ogrenci(string ad, bool profil = true, Func<AnketSorusu, (int Deger, int Agirlik)>? cevap = null, Rol rol = Rol.Ogrenci)
    {
        var kullanici = _kullanicilar.Ekle(new Kullanici
        {
            Eposta = $"{ad}@ornek.test",
            AdSoyad = ad,
            ParolaHash = VeriTohumlayici.GirisKapaliHash,
            Rol = rol,
            OlusturmaTarihi = DateTime.UtcNow,
            Profil = profil ? new Profil { Bolum = "Muhasebe", Sehir = "Bingöl", DogumYili = 2005 } : null,
        });
        if (cevap is not null)
        {
            _oturum.Ac(kullanici.Id, ad, rol);
            _anket.CevaplariKaydet(_anketDeposu.AktifSorular().Select(s =>
            {
                var (deger, agirlik) = cevap(s);
                return new CevapGirdisi(s.Id, deger, agirlik);
            }).ToList());
        }
        return kullanici.Id;
    }

    private void GirisYap(int id) => _oturum.Ac(id, "Test", Rol.Ogrenci);

    [Fact]
    public void Ayni_cevaplar_yuzde_yuz_zit_cevaplar_yuzde_sifir_verir()
    {
        var ben = Ogrenci("ben", cevap: _ => (1, 3));
        Ogrenci("ayni", cevap: _ => (1, 3));
        Ogrenci("zit", cevap: _ => (5, 3));
        GirisYap(ben);

        var sonuc = _adaylar.Adaylar();

        Assert.Equal(100, sonuc.Single(a => a.AdSoyad == "ayni").UyumYuzdesi, 5);
        Assert.Equal(0, sonuc.Single(a => a.AdSoyad == "zit").UyumYuzdesi, 5);
    }

    [Fact]
    public void Skor_iki_yonde_de_aynidir()
    {
        var a = Ogrenci("a", cevap: s => (1 + s.Sira % 5, 1 + s.Sira % 3));
        var b = Ogrenci("b", cevap: s => (1 + s.Sira * 2 % 5, 5 - s.Sira % 4));

        GirisYap(a);
        var ab = _adaylar.Adaylar().Single().UyumYuzdesi;
        GirisYap(b);
        var ba = _adaylar.Adaylar().Single().UyumYuzdesi;

        Assert.Equal(ab, ba, 8);
    }

    [Fact]
    public void Onem_derecesi_skoru_etkiler()
    {
        // Ilk soruda tam uyusmazlik, digerlerinde tam uyum.
        var ben = Ogrenci("ben", cevap: s => (3, s.Sira == 1 ? 5 : 1));
        Ogrenci("onemli", cevap: s => (s.Sira == 1 ? 5 : 3, s.Sira == 1 ? 5 : 1));
        GirisYap(ben);
        var ilkSoruOnemli = _adaylar.Adaylar().Single().UyumYuzdesi;

        var ben2 = Ogrenci("ben2", cevap: s => (3, s.Sira == 1 ? 1 : 5));
        GirisYap(ben2);
        var ilkSoruOnemsiz = _adaylar.Adaylar().Single(a => a.AdSoyad == "onemli").UyumYuzdesi;

        // Ayni uyusmazlik onemli soruda skoru daha cok dusurur (1 - 0.5 * agirlik payi).
        Assert.True(ilkSoruOnemli < ilkSoruOnemsiz);
    }

    [Fact]
    public void En_uyumlu_aday_basta_gelir_ve_kategoriler_belirlenir()
    {
        var ben = Ogrenci("ben", cevap: _ => (3, 3));
        Ogrenci("uzak", cevap: _ => (5, 3));
        Ogrenci("yakin", cevap: s => (s.Kategori == SoruKategorisi.Temizlik ? 5 : 3, 3));
        GirisYap(ben);

        var sonuc = _adaylar.Adaylar();

        Assert.Equal("yakin", sonuc[0].AdSoyad);
        Assert.Equal(SoruKategorisi.Temizlik, sonuc[0].EnFarkliKategori);
        Assert.NotEqual(SoruKategorisi.Temizlik, sonuc[0].EnUyumluKategori);
    }

    [Fact]
    public void Kendisi_yonetici_ve_eksik_kullanicilar_listelenmez()
    {
        var ben = Ogrenci("ben", cevap: _ => (3, 3));
        Ogrenci("yonetici", cevap: _ => (3, 3), rol: Rol.Yonetici);
        Ogrenci("anketsiz");
        Ogrenci("profilsiz", profil: false, cevap: _ => (3, 3));
        Ogrenci("tam", cevap: _ => (3, 3));
        GirisYap(ben);

        var sonuc = _adaylar.Adaylar();

        Assert.Equal("tam", Assert.Single(sonuc).AdSoyad);
    }

    [Fact]
    public void Anketini_tamamlamayan_adaylari_goremez()
    {
        var ben = Ogrenci("ben");
        Ogrenci("tam", cevap: _ => (3, 3));
        GirisYap(ben);

        Assert.Throws<IsKuraliHatasi>(() => _adaylar.Adaylar());
    }

    [Fact]
    public void Girissiz_erisim_engellenir()
    {
        _oturum.Kapat();

        Assert.Throws<YetkisizErisimHatasi>(() => _adaylar.Adaylar());
    }

    [Fact]
    public void Yas_dogum_yilindan_hesaplanir_ve_eposta_sonucta_yer_almaz()
    {
        var ben = Ogrenci("ben", cevap: _ => (3, 3));
        Ogrenci("tam", cevap: _ => (3, 3));
        GirisYap(ben);

        var aday = _adaylar.Adaylar().Single();

        Assert.Equal(2026 - 2005, aday.Yas);
        Assert.DoesNotContain(typeof(AdayOzeti).GetProperties(), p => p.Name is "Eposta" or "DogumYili" or "ParolaHash");
    }
}
