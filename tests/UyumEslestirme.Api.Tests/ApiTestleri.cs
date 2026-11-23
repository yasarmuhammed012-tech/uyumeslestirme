using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UyumEslestirme.Business;
using UyumEslestirme.Data;
using UyumEslestirme.Domain;

namespace UyumEslestirme.Api.Tests;

// Sunucu tum siniflar icin bir kez acilir; her test kendine ozgu e-postalarla calisir.
public sealed class ApiFikstur : IDisposable
{
    public ApiFabrikasi Fabrika { get; } = new();

    public void Dispose()
    {
        Fabrika.Dispose();
        Fabrika.DosyayiSil();
    }
}

public sealed class ApiTestleri(ApiFikstur f) : IClassFixture<ApiFikstur>
{
    private readonly ApiFabrikasi _api = f.Fabrika;
    private static string Eposta(string ad) => $"{ad}-{Guid.NewGuid():N}@ornek.test";

    [Fact]
    public async Task Iki_ogrenci_eslesir_eposta_ancak_kabulden_sonra_gorunur()
    {
        var (ayseEposta, aliEposta) = (Eposta("ayse"), Eposta("ali"));
        var ayse = _api.Istemci();
        var ali = _api.Istemci();
        await ayse.HazirOgrenci(ayseEposta, "Ayşe Deneme");
        await ali.HazirOgrenci(aliEposta, "Ali Deneme");

        // Ayse, Ali'yi Arkadas bul listesinde gorur; listede e-posta yoktur.
        var adaylarYaniti = await ayse.Http.GetStringAsync("/api/adaylar");
        Assert.DoesNotContain(aliEposta, adaylarYaniti);
        var aliAday = (await ayse.Al<List<AdayOzeti>>("/api/adaylar")).Single(a => a.AdSoyad == "Ali Deneme");
        Assert.Equal(100, aliAday.UyumYuzdesi);
        Assert.Equal(AdayDurumu.Yok, aliAday.Durum);

        await Istemci.Basarili(await ayse.Http.PostAsJsonAsync("/api/eslesmeler", new IstekGonderIstegi(aliAday.KullaniciId)));

        // Bekleyen istekte iki taraf da birbirinin e-postasini goremez.
        var aliListe = await ali.Al<EslesmeListeleri>("/api/eslesmeler");
        var gelen = Assert.Single(aliListe.GelenIstekler);
        Assert.Equal("Ayşe Deneme", gelen.AdSoyad);
        Assert.Null(gelen.Eposta);
        Assert.Null(Assert.Single((await ayse.Al<EslesmeListeleri>("/api/eslesmeler")).GonderilenIstekler).Eposta);

        await Istemci.Basarili(await ali.Http.PostAsync($"/api/eslesmeler/{gelen.EslesmeId}/kabul", null));

        // Karsilikli kabulden sonra e-postalar gorunur.
        Assert.Equal(ayseEposta, Assert.Single((await ali.Al<EslesmeListeleri>("/api/eslesmeler")).Eslesmeler).Eposta);
        Assert.Equal(aliEposta, Assert.Single((await ayse.Al<EslesmeListeleri>("/api/eslesmeler")).Eslesmeler).Eposta);
        Assert.Equal(AdayDurumu.Eslesti, (await ayse.Al<List<AdayOzeti>>("/api/adaylar")).Single(a => a.AdSoyad == "Ali Deneme").Durum);
    }

    [Theory]
    [InlineData("GET", "/api/profil")]
    [InlineData("GET", "/api/adaylar")]
    [InlineData("GET", "/api/eslesmeler")]
    [InlineData("GET", "/api/anket/sorular")]
    [InlineData("POST", "/api/eslesmeler/1/kabul")]
    [InlineData("GET", "/api/yonetim/kullanicilar")]
    public async Task Token_olmadan_istek_401_doner(string yontem, string adres)
    {
        var y = await _api.CreateClient().SendAsync(new HttpRequestMessage(new HttpMethod(yontem), adres));
        Assert.Equal(HttpStatusCode.Unauthorized, y.StatusCode);
    }

    [Fact]
    public async Task Sahte_imzali_token_401_doner()
    {
        var ogrenci = _api.Istemci();
        var eposta = Eposta("sahte");
        await ogrenci.KayitOl(eposta);
        var giris = await ogrenci.GirisYap(eposta);

        // Govdeyi degistirip imzayi oldugu gibi birakmak: sunucu imzayi dogrulayamaz.
        var parcalar = giris.Token.Split('.');
        parcalar[1] = parcalar[1][..^2] + (parcalar[1][^2..] == "AA" ? "BB" : "AA");
        ogrenci.Http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", string.Join('.', parcalar));
        Assert.Equal(HttpStatusCode.Unauthorized, (await ogrenci.Http.GetAsync("/api/profil")).StatusCode);
    }

    [Fact]
    public async Task Ogrenci_yonetim_ucunda_403_yonetici_200_alir()
    {
        var ogrenci = _api.Istemci();
        var eposta = Eposta("ogr");
        await ogrenci.KayitOl(eposta);
        await ogrenci.GirisYap(eposta);
        Assert.Equal(HttpStatusCode.Forbidden, (await ogrenci.Http.GetAsync("/api/yonetim/kullanicilar")).StatusCode);

        var yonetici = _api.Istemci();
        var giris = await yonetici.GirisYap(ApiFabrikasi.YoneticiEposta, ApiFabrikasi.YoneticiParola);
        Assert.Equal(Rol.Yonetici, giris.Rol);
        var json = await yonetici.Http.GetStringAsync("/api/yonetim/kullanicilar");
        Assert.Contains(eposta, json);
        Assert.DoesNotContain("pbkdf2", json);
        Assert.DoesNotContain("parola", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Yonetici_ogrenci_islemlerini_yapamaz_403()
    {
        var yonetici = _api.Istemci();
        await yonetici.GirisYap(ApiFabrikasi.YoneticiEposta, ApiFabrikasi.YoneticiParola);
        var y = await yonetici.Http.PostAsJsonAsync("/api/eslesmeler", new IstekGonderIstegi(1));
        Assert.Equal(HttpStatusCode.Forbidden, y.StatusCode);
    }

    [Fact]
    public async Task Kullanici_kimligi_govdeden_degil_tokendan_okunur()
    {
        var a = _api.Istemci();
        var b = _api.Istemci();
        var (ea, eb) = (Eposta("a"), Eposta("b"));
        await a.HazirOgrenci(ea, "Kişi A");
        await b.HazirOgrenci(eb, "Kişi B");
        var bId = (await a.Al<List<AdayOzeti>>("/api/adaylar")).Single(x => x.AdSoyad == "Kişi B").KullaniciId;

        // A, govdeye B'nin kimligini ekleyerek profil gonderir: yalnizca A'nin profili degisir.
        var govde = new { kullaniciId = bId, id = bId, bolum = "Muhasebe", sehir = "Elazığ", dogumYili = 2003, hakkinda = "değişti", gorunur = true };
        await Istemci.Basarili(await a.Http.PutAsJsonAsync("/api/profil", govde));

        Assert.Equal("Elazığ", (await a.Al<ProfilBilgisi>("/api/profil")).Sehir);
        Assert.Equal("Bingöl", (await b.Al<ProfilBilgisi>("/api/profil")).Sehir);
    }

    [Fact]
    public async Task Gizli_profil_listelenmez_ve_olmayan_kisiyle_ayni_mesaji_verir()
    {
        var ben = _api.Istemci();
        var gizli = _api.Istemci();
        await ben.HazirOgrenci(Eposta("ben"), "Görünen Kişi");
        await gizli.HazirOgrenci(Eposta("gizli"), "Gizli Kişi");
        await Istemci.Basarili(await gizli.Http.PutAsJsonAsync("/api/profil/gorunurluk", new GorunurlukIstegi(false)));
        Assert.False((await gizli.Al<ProfilBilgisi>("/api/profil")).Gorunur);

        Assert.DoesNotContain(await ben.Al<List<AdayOzeti>>("/api/adaylar"), a => a.AdSoyad == "Gizli Kişi");

        // Gizli kisinin kimligi bilinse bile istek gonderilemez; mesaj olmayan kisiyle aynidir.
        int gizliId;
        using (var kapsam = _api.Services.CreateScope())
            gizliId = kapsam.ServiceProvider.GetRequiredService<UygulamaDbContext>().Kullanicilar.Single(k => k.AdSoyad == "Gizli Kişi").Id;

        var gizliyeMesaj = await Istemci.HataMesaji(
            await ben.Http.PostAsJsonAsync("/api/eslesmeler", new IstekGonderIstegi(gizliId)), HttpStatusCode.BadRequest);
        var olmayanaMesaj = await Istemci.HataMesaji(
            await ben.Http.PostAsJsonAsync("/api/eslesmeler", new IstekGonderIstegi(999_999)), HttpStatusCode.BadRequest);
        Assert.Equal("Bu kişiye istek gönderilemez.", gizliyeMesaj);
        Assert.Equal(gizliyeMesaj, olmayanaMesaj);
    }

    [Fact]
    public async Task Bes_hatali_giristen_sonra_hesap_kilitlenir_mesaj_olmayan_hesapla_ayni()
    {
        var istemci = _api.Istemci();
        var eposta = Eposta("kilit");
        await istemci.KayitOl(eposta);

        for (var i = 0; i < KimlikServisi.AzamiDeneme; i++)
            await Istemci.HataMesaji(await istemci.GirisDene(eposta, "yanlis123"), HttpStatusCode.BadRequest);

        // Dogru parola da artik kabul edilmez; yanit olmayan bir hesaptan ayirt edilemez.
        var kilitli = await Istemci.HataMesaji(await istemci.GirisDene(eposta, "Parola123"), HttpStatusCode.BadRequest);
        var olmayan = await Istemci.HataMesaji(await istemci.GirisDene(Eposta("yok"), "Parola123"), HttpStatusCode.BadRequest);
        Assert.Equal(olmayan, kilitli);

        using var kapsam = _api.Services.CreateScope();
        var kullanici = kapsam.ServiceProvider.GetRequiredService<UygulamaDbContext>().Kullanicilar.AsNoTracking().Single(k => k.Eposta == eposta);
        Assert.True(kullanici.KilitBitis > DateTime.UtcNow.AddMinutes(14));
    }

    [Fact]
    public async Task Parola_sunucuda_600_bin_tekrarla_hashlenir()
    {
        var eposta = Eposta("hash");
        await _api.Istemci().KayitOl(eposta);

        using var kapsam = _api.Services.CreateScope();
        var hash = kapsam.ServiceProvider.GetRequiredService<UygulamaDbContext>().Kullanicilar.Single(k => k.Eposta == eposta).ParolaHash;
        Assert.StartsWith("pbkdf2-sha256$600000$", hash);
        Assert.DoesNotContain("Parola123", hash);
    }

    [Fact]
    public async Task En_fazla_10_bekleyen_istek_gonderilebilir()
    {
        var ben = _api.Istemci();
        await ben.HazirOgrenci(Eposta("cok"), "Çok İstekli");
        var hedefler = new List<int>();
        for (var i = 0; i <= EslesmeServisi.AzamiBekleyenIstek; i++)
        {
            var ad = $"Hedef {Guid.NewGuid():N}";
            await _api.Istemci().HazirOgrenci(Eposta("hedef"), ad);
            hedefler.Add((await ben.Al<List<AdayOzeti>>("/api/adaylar")).Single(a => a.AdSoyad == ad).KullaniciId);
        }

        foreach (var id in hedefler.Take(EslesmeServisi.AzamiBekleyenIstek))
            await Istemci.Basarili(await ben.Http.PostAsJsonAsync("/api/eslesmeler", new IstekGonderIstegi(id)));

        var mesaj = await Istemci.HataMesaji(
            await ben.Http.PostAsJsonAsync("/api/eslesmeler", new IstekGonderIstegi(hedefler[^1])), HttpStatusCode.BadRequest);
        Assert.Contains("en fazla 10", mesaj);
    }

    [Fact]
    public async Task Istegi_yalnizca_alici_yanitlar_baskasi_403_alir()
    {
        var (a, b, c) = (_api.Istemci(), _api.Istemci(), _api.Istemci());
        await a.HazirOgrenci(Eposta("a"), "Yanıt A");
        await b.HazirOgrenci(Eposta("b"), "Yanıt B");
        await c.HazirOgrenci(Eposta("c"), "Yanıt C");
        var bId = (await a.Al<List<AdayOzeti>>("/api/adaylar")).Single(x => x.AdSoyad == "Yanıt B").KullaniciId;
        await Istemci.Basarili(await a.Http.PostAsJsonAsync("/api/eslesmeler", new IstekGonderIstegi(bId)));
        var eslesmeId = (await a.Al<EslesmeListeleri>("/api/eslesmeler")).GonderilenIstekler.Single().EslesmeId;

        Assert.Equal(HttpStatusCode.Forbidden, (await c.Http.PostAsync($"/api/eslesmeler/{eslesmeId}/kabul", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await a.Http.PostAsync($"/api/eslesmeler/{eslesmeId}/kabul", null)).StatusCode);

        // Geri cekme ve reddetme
        await Istemci.Basarili(await a.Http.DeleteAsync($"/api/eslesmeler/{eslesmeId}"));
        Assert.Empty((await b.Al<EslesmeListeleri>("/api/eslesmeler")).GelenIstekler);
    }

    [Fact]
    public async Task Cikistan_sonra_ayni_token_gecersizdir()
    {
        var istemci = _api.Istemci();
        var eposta = Eposta("cikis");
        await istemci.KayitOl(eposta);
        await istemci.GirisYap(eposta);
        Assert.Equal(HttpStatusCode.NoContent, (await istemci.Http.GetAsync("/api/profil")).StatusCode);

        await Istemci.Basarili(await istemci.Http.PostAsync("/api/kimlik/cikis", null));
        Assert.Equal(HttpStatusCode.Unauthorized, (await istemci.Http.GetAsync("/api/profil")).StatusCode);
    }

    [Fact]
    public async Task Gecersiz_girdi_400_ve_aciklayici_mesaj_doner()
    {
        var istemci = _api.Istemci();
        var mesaj = await Istemci.HataMesaji(
            await istemci.Http.PostAsJsonAsync("/api/kimlik/kayit", new KayitIstegi("A", "gecersiz", "kisa", "kisa")), HttpStatusCode.BadRequest);
        Assert.Equal("Ad soyad 2-100 karakter olmalıdır.", mesaj);

        // Eksik alan (null parola) servise ulasmadan reddedilir.
        var eksik = await istemci.Http.PostAsJsonAsync("/api/kimlik/giris", new { eposta = "x@ornek.test" });
        Assert.Equal(HttpStatusCode.BadRequest, eksik.StatusCode);
    }
}

// Ayri bir sunucu: kayitlarin yeniden baslatmadan sonra da durdugunu dogrular.
public sealed class KalicilikTestleri
{
    [Fact]
    public async Task Sunucu_yeniden_baslasa_da_kullanici_ve_eslesme_kalir()
    {
        string dosya;
        int eslesmeId;
        using (var ilk = new ApiFabrikasi())
        {
            dosya = ilk.DbDosyasi;
            var a = ilk.Istemci();
            var b = ilk.Istemci();
            await a.HazirOgrenci("kalici-a@ornek.test", "Kalıcı A");
            await b.HazirOgrenci("kalici-b@ornek.test", "Kalıcı B");
            var bId = (await a.Al<List<AdayOzeti>>("/api/adaylar")).Single().KullaniciId;
            await Istemci.Basarili(await a.Http.PostAsJsonAsync("/api/eslesmeler", new IstekGonderIstegi(bId)));
            eslesmeId = (await b.Al<EslesmeListeleri>("/api/eslesmeler")).GelenIstekler.Single().EslesmeId;
        }

        var ikinci = new ApiFabrikasi(dosya);
        try
        {
            var b = ikinci.Istemci();
            await b.GirisYap("kalici-b@ornek.test");
            var gelen = Assert.Single((await b.Al<EslesmeListeleri>("/api/eslesmeler")).GelenIstekler);
            Assert.Equal(eslesmeId, gelen.EslesmeId);
            Assert.Equal("Kalıcı A", gelen.AdSoyad);
        }
        finally
        {
            ikinci.Dispose();
            ikinci.DosyayiSil();
        }
    }
}
