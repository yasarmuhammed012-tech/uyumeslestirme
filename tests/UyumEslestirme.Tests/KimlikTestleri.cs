using UyumEslestirme.Business;
using UyumEslestirme.Data;
using UyumEslestirme.Domain;

namespace UyumEslestirme.Tests;

public sealed class KimlikTestleri : IDisposable
{
    private const string Parola = "Gizli1234";

    private readonly TestVeritabani _t = new();
    private readonly SahteSaat _saat = new();
    private readonly OturumYoneticisi _oturum;
    private readonly KullaniciDeposu _depo;
    private readonly KimlikServisi _kimlik;

    public KimlikTestleri()
    {
        _oturum = new OturumYoneticisi(_saat);
        _depo = new KullaniciDeposu(_t.Db);
        _kimlik = new KimlikServisi(_depo, _oturum, _saat);
    }

    public void Dispose() => _t.Dispose();

    private void Kaydet(string eposta = "a@ornek.test") => _kimlik.KayitOl("Test Kişi", eposta, Parola, Parola);

    [Fact]
    public void Parola_duz_metin_saklanmaz_ve_her_kayitta_tuz_farklidir()
    {
        Kaydet("a@ornek.test");
        Kaydet("b@ornek.test");

        var a = _depo.EpostaIleBul("a@ornek.test")!.ParolaHash;
        var b = _depo.EpostaIleBul("b@ornek.test")!.ParolaHash;

        Assert.DoesNotContain(Parola, a);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Hash_dogru_parolayi_kabul_eder_yanlisi_ve_bozuk_kaydi_reddeder()
    {
        var hash = ParolaHasleyici.Hashle(Parola);

        Assert.True(ParolaHasleyici.Dogrula(Parola, hash));
        Assert.False(ParolaHasleyici.Dogrula("Yanlis1234", hash));
        Assert.False(ParolaHasleyici.Dogrula(Parola, VeriTohumlayici.GirisKapaliHash));
        Assert.False(ParolaHasleyici.Dogrula(Parola, "pbkdf2-sha256$0$AA==$AA=="));
        Assert.False(ParolaHasleyici.Dogrula(Parola, "pbkdf2-sha256$10$!!$!!"));
    }

    [Theory]
    [InlineData("X", "a@ornek.test", Parola, Parola)]
    [InlineData("Test Kişi", "gecersiz", Parola, Parola)]
    [InlineData("Test Kişi", "a@ornek.test", Parola, "Baska1234")]
    [InlineData("Test Kişi", "a@ornek.test", "kisa1", "kisa1")]
    [InlineData("Test Kişi", "a@ornek.test", "sadeceharf", "sadeceharf")]
    public void Gecersiz_kayit_reddedilir(string ad, string eposta, string parola, string tekrar)
    {
        Assert.Throws<IsKuraliHatasi>(() => _kimlik.KayitOl(ad, eposta, parola, tekrar));
        Assert.Empty(_depo.Listele());
    }

    [Fact]
    public void Ayni_eposta_buyuk_kucuk_harf_farkiyla_da_kayit_olamaz()
    {
        Kaydet("a@ornek.test");

        Assert.Throws<IsKuraliHatasi>(() => Kaydet("A@ORNEK.TEST"));
    }

    [Fact]
    public void Kayit_olan_her_zaman_ogrencidir()
    {
        Kaydet();

        Assert.Equal(Rol.Ogrenci, _depo.EpostaIleBul("a@ornek.test")!.Rol);
    }

    [Fact]
    public void Dogru_giris_oturum_acar_cikis_kapatir()
    {
        Kaydet();

        _kimlik.GirisYap("A@ornek.test ", Parola);
        Assert.True(_oturum.Acik);

        _kimlik.CikisYap();
        Assert.False(_oturum.Acik);
    }

    [Fact]
    public void Olmayan_kullanici_ile_yanlis_parola_ayni_hatayi_verir()
    {
        Kaydet();

        var yanlisParola = Assert.Throws<IsKuraliHatasi>(() => _kimlik.GirisYap("a@ornek.test", "Yanlis1234"));
        var olmayan = Assert.Throws<IsKuraliHatasi>(() => _kimlik.GirisYap("yok@ornek.test", "Yanlis1234"));

        Assert.Equal(yanlisParola.Message, olmayan.Message);
        Assert.False(_oturum.Acik);
    }

    [Fact]
    public void Bes_basarisiz_denemeden_sonra_hesap_kilitlenir_sure_bitince_acilir()
    {
        Kaydet();
        for (var i = 0; i < KimlikServisi.AzamiDeneme; i++)
            Assert.Throws<IsKuraliHatasi>(() => _kimlik.GirisYap("a@ornek.test", "Yanlis1234"));

        // Dogru parola bile kilit suresince kabul edilmez.
        Assert.Throws<IsKuraliHatasi>(() => _kimlik.GirisYap("a@ornek.test", Parola));
        Assert.False(_oturum.Acik);

        _saat.Ilerlet(KimlikServisi.KilitSuresi + TimeSpan.FromSeconds(1));
        _kimlik.GirisYap("a@ornek.test", Parola);
        Assert.True(_oturum.Acik);
    }

    [Fact]
    public void Basarili_giris_deneme_sayacini_sifirlar()
    {
        Kaydet();
        for (var i = 0; i < KimlikServisi.AzamiDeneme - 1; i++)
            Assert.Throws<IsKuraliHatasi>(() => _kimlik.GirisYap("a@ornek.test", "Yanlis1234"));

        _kimlik.GirisYap("a@ornek.test", Parola);

        Assert.Equal(0, _depo.EpostaIleBul("a@ornek.test")!.BasarisizGiris);
    }

    [Fact]
    public void Oturum_suresi_dolunca_kapanir()
    {
        Kaydet();
        _kimlik.GirisYap("a@ornek.test", Parola);

        _saat.Ilerlet(OturumYoneticisi.OturumSuresi + TimeSpan.FromSeconds(1));

        Assert.False(_oturum.Acik);
        Assert.Throws<YetkisizErisimHatasi>(_oturum.GirisGerekli);
    }

    [Fact]
    public void Yonetim_islemi_girissiz_ve_ogrenci_icin_engellenir_yonetici_icin_calisir()
    {
        var yonetim = new YonetimServisi(_depo, _oturum);
        Assert.Throws<YetkisizErisimHatasi>(yonetim.KullanicilariListele);

        Kaydet();
        _kimlik.GirisYap("a@ornek.test", Parola);
        Assert.Throws<YetkisizErisimHatasi>(yonetim.KullanicilariListele);

        _oturum.Ac(1, "Yönetici", Rol.Yonetici);
        Assert.Single(yonetim.KullanicilariListele());
    }
}
