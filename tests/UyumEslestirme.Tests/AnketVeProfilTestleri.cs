using UyumEslestirme.Business;
using UyumEslestirme.Data;
using UyumEslestirme.Domain;

namespace UyumEslestirme.Tests;

public sealed class AnketVeProfilTestleri : IDisposable
{
    private readonly TestVeritabani _t = new();
    private readonly SahteSaat _saat = new();
    private readonly OturumYoneticisi _oturum;
    private readonly AnketDeposu _anketDeposu;
    private readonly AnketServisi _anket;
    private readonly ProfilServisi _profil;
    private readonly int _ali;
    private readonly int _ayse;

    public AnketVeProfilTestleri()
    {
        _oturum = new OturumYoneticisi(_saat);
        _anketDeposu = new AnketDeposu(_t.Db);
        _anket = new AnketServisi(_anketDeposu, _oturum);
        _profil = new ProfilServisi(new Depo<Profil>(_t.Db), _oturum, _saat);
        VeriTohumlayici.SorulariTohumla(_t.Db);
        _ali = KullaniciEkle("kisi1@ornek.test");
        _ayse = KullaniciEkle("kisi2@ornek.test");
        _oturum.Ac(_ali, "Kişi 1", Rol.Ogrenci);
    }

    public void Dispose() => _t.Dispose();

    private int KullaniciEkle(string eposta) => new KullaniciDeposu(_t.Db).Ekle(new Kullanici
    {
        Eposta = eposta,
        AdSoyad = "Test",
        ParolaHash = VeriTohumlayici.GirisKapaliHash,
        OlusturmaTarihi = DateTime.UtcNow,
    }).Id;

    private List<CevapGirdisi> TamCevaplar(int deger = 3, int agirlik = 3) =>
        _anketDeposu.AktifSorular().Select(s => new CevapGirdisi(s.Id, deger, agirlik)).ToList();

    [Fact]
    public void Tam_anket_kaydedilir_ve_tamamlandi_sayilir()
    {
        Assert.False(_anket.TamamlandiMi());

        _anket.CevaplariKaydet(TamCevaplar());

        Assert.True(_anket.TamamlandiMi());
        Assert.Equal(10, _anket.BenimCevaplarim().Count);
    }

    [Fact]
    public void Eksik_cevapli_anket_reddedilir()
    {
        var cevaplar = TamCevaplar();
        cevaplar.RemoveAt(0);

        Assert.Throws<IsKuraliHatasi>(() => _anket.CevaplariKaydet(cevaplar));
        Assert.Empty(_anket.BenimCevaplarim());
    }

    [Theory]
    [InlineData(0, 3)]
    [InlineData(6, 3)]
    [InlineData(3, 0)]
    [InlineData(3, 6)]
    public void Olcek_disi_deger_reddedilir(int deger, int agirlik)
    {
        Assert.Throws<IsKuraliHatasi>(() => _anket.CevaplariKaydet(TamCevaplar(deger, agirlik)));
    }

    [Fact]
    public void Ayni_sorunun_tekrari_reddedilir()
    {
        var cevaplar = TamCevaplar();
        cevaplar[0] = cevaplar[1];

        Assert.Throws<IsKuraliHatasi>(() => _anket.CevaplariKaydet(cevaplar));
    }

    [Fact]
    public void Olmayan_soru_reddedilir()
    {
        var cevaplar = TamCevaplar();
        cevaplar[0] = new CevapGirdisi(9999, 3, 3);

        Assert.Throws<IsKuraliHatasi>(() => _anket.CevaplariKaydet(cevaplar));
    }

    [Fact]
    public void Pasif_soru_zorunlu_degildir()
    {
        var pasif = _t.Db.AnketSorulari.First();
        pasif.Aktif = false;
        _t.Db.SaveChanges();

        _anket.CevaplariKaydet(TamCevaplar());

        Assert.Equal(9, _anket.BenimCevaplarim().Count);
        Assert.True(_anket.TamamlandiMi());
    }

    [Fact]
    public void Yeniden_kaydetmek_eski_cevaplari_degistirir()
    {
        _anket.CevaplariKaydet(TamCevaplar(deger: 1));
        _anket.CevaplariKaydet(TamCevaplar(deger: 5));

        var cevaplar = _anket.BenimCevaplarim();
        Assert.Equal(10, cevaplar.Count);
        Assert.All(cevaplar, c => Assert.Equal(5, c.Deger));
    }

    [Fact]
    public void Cevaplar_yalnizca_oturumdaki_kullaniciya_yazilir_ve_ondan_okunur()
    {
        _anket.CevaplariKaydet(TamCevaplar(deger: 1));

        _oturum.Ac(_ayse, "Kişi 2", Rol.Ogrenci);
        Assert.Empty(_anket.BenimCevaplarim());
        _anket.CevaplariKaydet(TamCevaplar(deger: 5));

        _oturum.Ac(_ali, "Kişi 1", Rol.Ogrenci);
        Assert.All(_anket.BenimCevaplarim(), c => Assert.Equal(1, c.Deger));
    }

    [Fact]
    public void Tercih_ozeti_onem_derecesine_gore_agirliklandirilir()
    {
        // Uyku kategorisinde iki soru: (cevap 5, onem 3) ve (cevap 1, onem 1). Diger sorular (3, 1).
        var cevaplar = TamCevaplar(deger: 3, agirlik: 1);
        var uyku = _anketDeposu.AktifSorular().Where(s => s.Kategori == SoruKategorisi.UykuDuzeni).Select(s => s.Id).ToList();
        cevaplar[cevaplar.FindIndex(c => c.SoruId == uyku[0])] = new CevapGirdisi(uyku[0], 5, 3);
        cevaplar[cevaplar.FindIndex(c => c.SoruId == uyku[1])] = new CevapGirdisi(uyku[1], 1, 1);
        _anket.CevaplariKaydet(cevaplar);

        var ozet = _anket.TercihOzeti().Single(o => o.Kategori == SoruKategorisi.UykuDuzeni);

        // Ortalama = (5*3 + 1*1) / 4 = 4; toplam onem = 4 + 8 = 12, pay = 4 / 12.
        Assert.Equal(4, ozet.Ortalama, 5);
        Assert.Equal(100.0 / 3, ozet.AgirlikYuzdesi, 5);
    }

    [Fact]
    public void Cevapsiz_kullanicinin_tercih_ozeti_bostur()
    {
        Assert.Empty(_anket.TercihOzeti());
    }

    [Fact]
    public void Girissiz_anket_ve_profil_islemleri_engellenir()
    {
        _oturum.Kapat();

        Assert.Throws<YetkisizErisimHatasi>(() => _anket.Sorular());
        Assert.Throws<YetkisizErisimHatasi>(() => _anket.CevaplariKaydet(TamCevaplar()));
        Assert.Throws<YetkisizErisimHatasi>(() => _profil.BenimProfilim());
    }

    [Fact]
    public void Profil_olusturulur_ve_guncellenir()
    {
        _profil.Kaydet(new ProfilGirdisi(" Muhasebe ", "Bingöl", 2005, null));
        Assert.Equal("Muhasebe", _profil.BenimProfilim()!.Bolum);

        _profil.Kaydet(new ProfilGirdisi("İşletme", "Elazığ", 2004, "  "));

        var profil = _profil.BenimProfilim()!;
        Assert.Equal("İşletme", profil.Bolum);
        Assert.Equal("Elazığ", profil.Sehir);
        Assert.Null(profil.Hakkinda);
    }

    [Theory]
    [InlineData("", "Bingöl", 2005)]
    [InlineData("Muhasebe", "", 2005)]
    [InlineData("Muhasebe", "Bingöl", 2011)] // 15 yas
    [InlineData("Muhasebe", "Bingöl", 1945)] // 81 yas
    public void Gecersiz_profil_reddedilir(string bolum, string sehir, int dogumYili)
    {
        Assert.Throws<IsKuraliHatasi>(() => _profil.Kaydet(new ProfilGirdisi(bolum, sehir, dogumYili, null)));
        Assert.Null(_profil.BenimProfilim());
    }

    [Theory]
    [InlineData(2010)] // 16 yas
    [InlineData(1946)] // 80 yas
    public void Yas_sinirlari_dahildir(int dogumYili)
    {
        _profil.Kaydet(new ProfilGirdisi("Muhasebe", "Bingöl", dogumYili, null));

        Assert.Equal(dogumYili, _profil.BenimProfilim()!.DogumYili);
    }

    [Fact]
    public void Baskasinin_profili_ogrenciye_kapali_yoneticiye_aciktir()
    {
        _profil.Kaydet(new ProfilGirdisi("Muhasebe", "Bingöl", 2005, null));

        _oturum.Ac(_ayse, "Kişi 2", Rol.Ogrenci);
        Assert.Throws<YetkisizErisimHatasi>(() => _profil.Goruntule(_ali));

        _oturum.Ac(_ayse, "Kişi 2", Rol.Yonetici);
        Assert.NotNull(_profil.Goruntule(_ali));
    }
}
