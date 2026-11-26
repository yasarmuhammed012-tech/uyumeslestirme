using System.Text.Json;
using UyumEslestirme.Business;
using UyumEslestirme.Domain;

namespace UyumEslestirme.Tests;

// Hafta 10: aciklamali uyum onerisi. Gercek dil modeli yerine SahteYapayZeka kullanilir;
// testler skorla birlestirmeyi, cikti dogrulamayi ve hatali/bos cikti yonetimini sinar.
public sealed class OneriTestleri : IDisposable
{
    private readonly TestOrtami _o = new();
    private readonly int _ben;
    private readonly int _ayni;     // her soruda ayni cevap: %100
    private readonly int _yakin;    // temizlik farkli, digerleri ayni
    private readonly int _uzak;     // her soruda 1 fark: %75
    private readonly int _dorduncu; // en dusuk skor: listeye girmez

    public OneriTestleri()
    {
        _ben = _o.Ogrenci("ben", cevap: _ => (3, 3));
        _ayni = _o.Ogrenci("Ayşe Aynı", cevap: _ => (3, 3));
        _yakin = _o.Ogrenci("Yakın Yasin", cevap: s => (s.Kategori == SoruKategorisi.Temizlik ? 1 : 3, 3));
        _uzak = _o.Ogrenci("Uzak Umut", cevap: _ => (4, 3));
        _dorduncu = _o.Ogrenci("Dördüncü Deniz", cevap: _ => (5, 3));
        _o.GirisYap(_ben);
    }

    public void Dispose() => _o.Dispose();

    // Kural tabanli oneriden, dogrulayicinin kabul edecegi bir yapay zeka yaniti uretir.
    private string GecerliYanit()
    {
        var kural = _o.YapayZekasizOneri.Oner().GetAwaiter().GetResult();
        return JsonSerializer.Serialize(new
        {
            oneriler = kural.Oneriler.Select((o, i) => new
            {
                kod = $"A{i + 1}",
                gerekce = "Bu kişiyle günlük düzeniniz çok benzer; " + o.Gerekce,
                guclu_yonler = o.GucluYonler.Select(k => k.ToString()).ToArray(),
                dikkat_konusu = o.DikkatKonusu?.ToString() ?? "Yok",
                dikkat_notu = o.DikkatNotu ?? "",
            }),
        }, Okunur);
    }

    // Turkce karakterler ş gibi kacislanmasin; testler metin uzerinde Replace yapabilsin.
    private static readonly JsonSerializerOptions Okunur = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    [Fact]
    public async Task Skor_siralamayi_belirler_yapay_zeka_gerekceyi_yazar()
    {
        _o.YapayZeka.Yanit = _ => GecerliYanit();

        var sonuc = await _o.Oneri.Oner();

        Assert.Equal(OneriKaynagi.YapayZeka, sonuc.Kaynak);
        Assert.Null(sonuc.Aciklama);
        Assert.Equal([_ayni, _yakin, _uzak], sonuc.Oneriler.Select(o => o.KullaniciId));
        Assert.Equal(100, sonuc.Oneriler[0].UyumYuzdesi);
        Assert.StartsWith("Bu kişiyle", sonuc.Oneriler[0].Gerekce);
        Assert.Equal(SoruKategorisi.Temizlik, sonuc.Oneriler[1].DikkatKonusu);
    }

    [Fact]
    public async Task Yapay_zekaya_kisisel_veri_gitmez()
    {
        _o.YapayZeka.Yanit = _ => GecerliYanit();
        await _o.Oneri.Oner();

        var istem = _o.YapayZeka.SonIstem!;
        Assert.DoesNotContain("Ayşe", istem);
        Assert.DoesNotContain("@ornek.test", istem);
        Assert.DoesNotContain("Muhasebe", istem);
        Assert.Contains("\"kod\": \"A1\"", istem);
    }

    [Fact]
    public async Task Istek_gonderilmis_kisi_yeniden_onerilmez()
    {
        _o.Eslesme.IstekGonder(_ayni);
        var sonuc = await _o.YapayZekasizOneri.Oner();
        Assert.Equal([_yakin, _uzak, _dorduncu], sonuc.Oneriler.Select(o => o.KullaniciId));
    }

    [Fact]
    public async Task Servis_hata_verirse_kural_tabanli_gerekce_ve_aciklama_doner()
    {
        _o.YapayZeka.Hata = new YapayZekaHatasi("bağlantı kurulamadı");

        var sonuc = await _o.Oneri.Oner();

        Assert.Equal(OneriKaynagi.KuralTabanli, sonuc.Kaynak);
        Assert.Contains("bağlantı kurulamadı", sonuc.Aciklama);
        Assert.Equal(3, sonuc.Oneriler.Count);
        Assert.Contains("%100", sonuc.Oneriler[0].Gerekce);
    }

    [Fact]
    public async Task Zaman_asiminda_kural_tabanliya_doner()
    {
        _o.YapayZeka.Gecikme = TimeSpan.FromSeconds(30);
        var servis = new OneriServisi(_o.Adaylar, new UyumServisi(_o.AnketDeposu, new Data.UyumSkoruDeposu(_o.Db), _o.Saat), _o.Oturum, _o.YapayZeka)
        {
            ZamanAsimi = TimeSpan.FromMilliseconds(50),
        };

        var sonuc = await servis.Oner();

        Assert.Equal(OneriKaynagi.KuralTabanli, sonuc.Kaynak);
        Assert.Contains("zamanında", sonuc.Aciklama);
    }

    [Theory]
    [InlineData("")]
    [InlineData("bu bir JSON değil")]
    [InlineData("{\"oneriler\": []}")]
    [InlineData("{\"baska\": 1}")]
    public async Task Bos_veya_hatali_cikti_kullanilmaz(string yanit)
    {
        _o.YapayZeka.Yanit = _ => yanit;
        var sonuc = await _o.Oneri.Oner();
        Assert.Equal(OneriKaynagi.KuralTabanli, sonuc.Kaynak);
        Assert.Contains("doğrulamadan geçmedi", sonuc.Aciklama);
    }

    [Fact]
    public async Task Uydurulmus_yuzde_iceren_cikti_reddedilir()
    {
        var gecerli = GecerliYanit();
        var bozuk = gecerli.Replace("Bu kişiyle günlük düzeniniz çok benzer;", "Uyumunuz %97;");
        Assert.NotEqual(gecerli, bozuk);
        _o.YapayZeka.Yanit = _ => bozuk;
        var sonuc = await _o.Oneri.Oner();
        Assert.Equal(OneriKaynagi.KuralTabanli, sonuc.Kaynak);
    }

    [Fact]
    public async Task Yapay_zeka_yoksa_kural_tabanli_calisir()
    {
        var sonuc = await _o.YapayZekasizOneri.Oner();
        Assert.Equal(OneriKaynagi.KuralTabanli, sonuc.Kaynak);
        Assert.Equal(0, _o.YapayZeka.CagriSayisi);
    }

    [Fact]
    public async Task Onerilecek_kimse_yoksa_yapay_zeka_cagrilmaz()
    {
        using var bos = new TestOrtami();
        var yalniz = bos.Ogrenci("yalniz", cevap: _ => (3, 3));
        bos.GirisYap(yalniz);

        var sonuc = await bos.Oneri.Oner();

        Assert.Empty(sonuc.Oneriler);
        Assert.Equal(0, bos.YapayZeka.CagriSayisi);
    }

    [Fact]
    public async Task Yonetici_oneri_alamaz()
    {
        var yonetici = _o.Ogrenci("yonetici", profil: false, rol: Rol.Yonetici);
        _o.GirisYap(yonetici, Rol.Yonetici);
        await Assert.ThrowsAsync<YetkisizErisimHatasi>(() => _o.Oneri.Oner());
    }
}

// Dogrulayicinin kurallari tek tek: gercekler elle kurulur.
public sealed class OneriDogrulayiciTestleri
{
    private static readonly AdayOzeti Aday = new(7, "Ad", "Bolum", "Sehir", 20, null, 82, SoruKategorisi.Gurultu, SoruKategorisi.Temizlik, AdayDurumu.Yok);

    private static readonly List<AdayGercekleri> Gercekler =
    [
        new("A1", Aday,
        [
            new(SoruKategorisi.UykuDuzeni, 90, 3, 3.5, 4),
            new(SoruKategorisi.Temizlik, 50, 5, 3, 4),
            new(SoruKategorisi.Sosyallik, 70, 2, 3, 2),
            new(SoruKategorisi.Gurultu, 100, 4, 4, 5),
            new(SoruKategorisi.Ders, 85, 4, 3.5, 3),
        ]),
    ];

    private static string Json(
        string kod = "A1",
        string gerekce = "Sessizlik isteğiniz aynı (%100) ve gece düzeniniz çok yakın; genel uyumunuz %82.",
        string[]? guclu = null,
        string dikkat = "Temizlik",
        string not = "Temizlik paylaşımını baştan konuşun.") =>
        JsonSerializer.Serialize(new
        {
            oneriler = new[]
            {
                new { kod, gerekce, guclu_yonler = guclu ?? ["Gurultu", "UykuDuzeni"], dikkat_konusu = dikkat, dikkat_notu = not },
            },
        });

    private static List<string> Hatalar(string json) => OneriDogrulayici.Dogrula(json, Gercekler).Hatalar;

    [Fact]
    public void Gerceklerle_uyumlu_cikti_kabul_edilir()
    {
        var sonuc = OneriDogrulayici.Dogrula(Json(), Gercekler);
        Assert.Empty(sonuc.Hatalar);
        var o = sonuc.Oneriler["A1"];
        Assert.Equal([SoruKategorisi.Gurultu, SoruKategorisi.UykuDuzeni], o.GucluYonler);
        Assert.Equal(SoruKategorisi.Temizlik, o.DikkatKonusu);
    }

    [Fact]
    public void Bilinmeyen_aday_kodu_reddedilir() => Assert.NotEmpty(Hatalar(Json(kod: "A9")));

    [Fact]
    public void Verideki_degerle_uyusmayan_yuzde_reddedilir() =>
        Assert.Contains(Hatalar(Json(gerekce: "Bu kişiyle %95 uyumlusunuz, sessizliği ikiniz de seviyorsunuz.")), h => h.Contains("%95"));

    [Fact]
    public void Bir_puanlik_yuvarlama_farki_kabul_edilir() =>
        Assert.Empty(Hatalar(Json(gerekce: "Genel uyumunuz %81 civarında; sessizlik isteğiniz tamamen aynı.")));

    [Theory]
    [InlineData("İletişim için ornek@site.com adresine yazın, çok uyumlusunuz.")]
    [InlineData("Bu kişiye 0532 123 45 67 numarasından ulaşabilirsiniz, uyumlusunuz.")]
    [InlineData("A1 adayı sessizlik konusunda sizinle tamamen aynı düşünüyor.")]
    public void Iletisim_bilgisi_veya_aday_kodu_reddedilir(string gerekce) => Assert.NotEmpty(Hatalar(Json(gerekce: gerekce)));

    [Fact]
    public void Dusuk_uyumlu_konu_guclu_yon_sayilamaz() =>
        Assert.Contains(Hatalar(Json(guclu: ["Temizlik"])), h => h.Contains("güçlü yön değil"));

    [Fact]
    public void Dusuk_uyumlu_konu_varken_dikkat_yok_denemez() =>
        Assert.Contains(Hatalar(Json(dikkat: "Yok", not: "")), h => h.Contains("atlanmış"));

    [Fact]
    public void Dikkat_konusu_en_dusuk_iki_kategoriden_olmali() =>
        Assert.NotEmpty(Hatalar(Json(dikkat: "Gurultu")));

    [Fact]
    public void Gecersiz_kategori_ve_cok_kisa_gerekce_reddedilir()
    {
        Assert.NotEmpty(Hatalar(Json(guclu: ["Yemek"])));
        Assert.NotEmpty(Hatalar(Json(gerekce: "Uyumlu.")));
    }

    [Fact]
    public void Eksik_aday_raporlanir()
    {
        var iki = Gercekler.Append(Gercekler[0] with { Kod = "A2" }).ToList();
        Assert.Contains(OneriDogrulayici.Dogrula(Json(), iki).Hatalar, h => h.Contains("A2"));
    }
}
