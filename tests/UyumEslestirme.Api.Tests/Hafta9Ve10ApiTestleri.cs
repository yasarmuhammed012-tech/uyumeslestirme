using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using UyumEslestirme.Api.Bildirim;
using UyumEslestirme.Business;
using UyumEslestirme.Domain;

namespace UyumEslestirme.Api.Tests;

// ntfy yerine: gonderilen isleri toplar; istenirse hata firlatir.
public sealed class SahteKanal : IHariciBildirimKanali
{
    public ConcurrentQueue<HariciBildirimIsi> Gonderilenler { get; } = new();
    public string? HataVer { get; set; }

    public Task Gonder(HariciBildirimIsi is_, CancellationToken iptal)
    {
        if (HataVer is not null)
            throw new BildirimGonderimHatasi(HataVer, kalici: true);
        Gonderilenler.Enqueue(is_);
        return Task.CompletedTask;
    }
}

// Claude yerine: istemdeki aday kodlarina gecerli bir yanit yazar.
public sealed partial class SahteClaude : IYapayZekaIstemcisi
{
    public bool Bozuk { get; set; }

    [GeneratedRegex("\"kod\": \"(A\\d)\"")]
    private static partial Regex KodKalibi();

    public Task<string> JsonUret(string sistem, string istem, string jsonSemasi, CancellationToken iptal)
    {
        if (Bozuk)
            return Task.FromResult("{\"oneriler\": \"bozuk\"}");
        var kodlar = KodKalibi().Matches(istem).Select(m => m.Groups[1].Value);
        return Task.FromResult(JsonSerializer.Serialize(new
        {
            oneriler = kodlar.Select(k => new
            {
                kod = k,
                gerekce = "Bu kişiyle bütün konularda aynı cevapları verdiniz; genel uyumunuz %100.",
                guclu_yonler = new[] { "Gurultu", "UykuDuzeni" },
                dikkat_konusu = "Yok",
                dikkat_notu = "",
            }),
        }));
    }
}

public sealed class Hafta9Fikstur : IDisposable
{
    public SahteKanal Kanal { get; } = new();
    public SahteClaude Claude { get; } = new();
    public ApiFabrikasi Fabrika { get; }

    public Hafta9Fikstur()
    {
        Fabrika = new ApiFabrikasi
        {
            SahteServisler = s =>
            {
                s.AddSingleton<IHariciBildirimKanali>(Kanal);
                s.AddSingleton<IYapayZekaIstemcisi>(Claude);
            },
        };
    }

    public void Dispose()
    {
        Fabrika.Dispose();
        Fabrika.DosyayiSil();
    }
}

public sealed class Hafta9Ve10ApiTestleri(Hafta9Fikstur f) : IClassFixture<Hafta9Fikstur>
{
    private const string Konu = "uyum-api-test-konusu";
    private readonly ApiFabrikasi _api = f.Fabrika;
    private static string Eposta(string ad) => $"{ad}-{Guid.NewGuid():N}@ornek.test";

    private async Task<(Istemci A, Istemci B, int BId)> IkiOgrenci()
    {
        var a = _api.Istemci();
        var b = _api.Istemci();
        var adB = $"B {Guid.NewGuid():N}";
        await a.HazirOgrenci(Eposta("a"), $"A {Guid.NewGuid():N}");
        await b.HazirOgrenci(Eposta("b"), adB);
        var bId = (await a.Al<List<AdayOzeti>>("/api/adaylar")).Single(x => x.AdSoyad == adB).KullaniciId;
        return (a, b, bId);
    }

    // Arka plandaki gonderici bitene kadar bekler.
    private static async Task<BildirimOzeti> Sonuclanana(Istemci i, BildirimTuru tur)
    {
        for (var d = 0; d < 50; d++)
        {
            var b = (await i.Al<List<BildirimOzeti>>("/api/bildirimler")).FirstOrDefault(x => x.Tur == tur);
            if (b is not null && b.HariciDurum != HariciGonderimDurumu.Bekliyor)
                return b;
            await Task.Delay(100);
        }
        throw new TimeoutException("Bildirim sonuçlanmadı.");
    }

    [Fact]
    public async Task Istek_ve_kabul_bildirimleri_olusur_ve_telefona_gonderilir()
    {
        var (a, b, bId) = await IkiOgrenci();
        await Istemci.Basarili(await b.Http.PutAsJsonAsync("/api/bildirimler/tercihler", new BildirimTercihleri(true, true, Konu)));

        await Istemci.Basarili(await a.Http.PostAsJsonAsync("/api/eslesmeler", new IstekGonderIstegi(bId)));

        Assert.Equal(1, (await b.Al<JsonElement>("/api/bildirimler/okunmamis")).GetProperty("sayi").GetInt32());
        var bildirim = await Sonuclanana(b, BildirimTuru.EslesmeIstegi);
        Assert.Equal(HariciGonderimDurumu.Gonderildi, bildirim.HariciDurum);
        Assert.Contains(f.Kanal.Gonderilenler, i => i.BildirimId == bildirim.Id && i.Konu == Konu);

        var gelen = Assert.Single((await b.Al<EslesmeListeleri>("/api/eslesmeler")).GelenIstekler);
        await Istemci.Basarili(await b.Http.PostAsync($"/api/eslesmeler/{gelen.EslesmeId}/kabul", null));
        Assert.Contains(await a.Al<List<BildirimOzeti>>("/api/bildirimler"), x => x.Tur == BildirimTuru.IstekKabul);

        await Istemci.Basarili(await b.Http.PostAsync("/api/bildirimler/okundu", null));
        Assert.Equal(0, (await b.Al<JsonElement>("/api/bildirimler/okunmamis")).GetProperty("sayi").GetInt32());
    }

    [Fact]
    public async Task Harici_servis_hata_verirse_islem_bozulmaz_hata_kaydedilir()
    {
        var (a, b, bId) = await IkiOgrenci();
        await Istemci.Basarili(await b.Http.PutAsJsonAsync("/api/bildirimler/tercihler", new BildirimTercihleri(true, true, Konu)));
        f.Kanal.HataVer = "Bildirim servisi anahtarı reddetti (401/403).";
        try
        {
            await Istemci.Basarili(await a.Http.PostAsJsonAsync("/api/eslesmeler", new IstekGonderIstegi(bId)));
            var bildirim = await Sonuclanana(b, BildirimTuru.EslesmeIstegi);
            Assert.Equal(HariciGonderimDurumu.Basarisiz, bildirim.HariciDurum);
            Assert.Contains("401", bildirim.HariciHata);
            Assert.Single((await b.Al<EslesmeListeleri>("/api/eslesmeler")).GelenIstekler);
        }
        finally
        {
            f.Kanal.HataVer = null;
        }
    }

    [Fact]
    public async Task Eslesenler_mesajlasir_eslesmeyen_400_alir()
    {
        var (a, b, bId) = await IkiOgrenci();
        var y = await a.Http.PostAsJsonAsync("/api/mesajlar", new MesajGonderIstegi(bId, "Merhaba"));
        Assert.Contains("eşleştiğiniz", await Istemci.HataMesaji(y, HttpStatusCode.BadRequest));

        await Istemci.Basarili(await a.Http.PostAsJsonAsync("/api/eslesmeler", new IstekGonderIstegi(bId)));
        var gelen = Assert.Single((await b.Al<EslesmeListeleri>("/api/eslesmeler")).GelenIstekler);
        await Istemci.Basarili(await b.Http.PostAsync($"/api/eslesmeler/{gelen.EslesmeId}/kabul", null));

        await Istemci.Basarili(await a.Http.PostAsJsonAsync("/api/mesajlar", new MesajGonderIstegi(bId, "Merhaba")));
        var m = Assert.Single(await b.Al<List<MesajOzeti>>($"/api/mesajlar/{gelen.KisiId}"));
        Assert.Equal("Merhaba", m.Icerik);
        Assert.Contains(await b.Al<List<BildirimOzeti>>("/api/bildirimler"), x => x.Tur == BildirimTuru.YeniMesaj);
    }

    [Fact]
    public async Task Gecersiz_konu_400_doner()
    {
        var (a, _, _) = await IkiOgrenci();
        var y = await a.Http.PutAsJsonAsync("/api/bildirimler/tercihler", new BildirimTercihleri(true, true, "kisa"));
        Assert.Contains("12-64", await Istemci.HataMesaji(y, HttpStatusCode.BadRequest));
    }

    [Fact]
    public async Task Oneriler_yapay_zeka_gerekcesiyle_doner()
    {
        var (a, _, _) = await IkiOgrenci();
        var sonuc = await a.Al<OneriSonucu>("/api/oneriler");
        Assert.Equal(OneriKaynagi.YapayZeka, sonuc.Kaynak);
        Assert.NotEmpty(sonuc.Oneriler);
        Assert.All(sonuc.Oneriler, o => Assert.Contains("%100", o.Gerekce));
    }

    [Fact]
    public async Task Bozuk_yapay_zeka_ciktisinda_kural_tabanli_doner()
    {
        var (a, _, _) = await IkiOgrenci();
        f.Claude.Bozuk = true;
        try
        {
            var sonuc = await a.Al<OneriSonucu>("/api/oneriler");
            Assert.Equal(OneriKaynagi.KuralTabanli, sonuc.Kaynak);
            Assert.NotEmpty(sonuc.Oneriler);
            Assert.Contains("doğrulamadan geçmedi", sonuc.Aciklama);
        }
        finally
        {
            f.Claude.Bozuk = false;
        }
    }

    [Theory]
    [InlineData("/api/bildirimler")]
    [InlineData("/api/oneriler")]
    [InlineData("/api/mesajlar/1")]
    public async Task Yeni_uclar_token_olmadan_401_doner(string adres) =>
        Assert.Equal(HttpStatusCode.Unauthorized, (await _api.CreateClient().GetAsync(adres)).StatusCode);
}
