using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using UyumEslestirme.Api.Bildirim;
using UyumEslestirme.Business;

namespace UyumEslestirme.Api.Tests;

// ntfy'a gercekten baglanmadan HTTP katmanini taklit eder: sirayla verilen yanitlari dondurur.
public sealed class SahteHttp(params Func<HttpRequestMessage, Task<HttpResponseMessage>>[] yanitlar) : HttpMessageHandler
{
    public List<(HttpRequestMessage Istek, string Govde)> Istekler { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
    {
        Istekler.Add((istek, istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal)));
        var sira = Math.Min(Istekler.Count - 1, yanitlar.Length - 1);
        var y = await yanitlar[sira](istek);
        iptal.ThrowIfCancellationRequested();
        return y;
    }

    public static Func<HttpRequestMessage, Task<HttpResponseMessage>> Kod(HttpStatusCode kod, string govde = "{}") =>
        _ => Task.FromResult(new HttpResponseMessage(kod) { Content = new StringContent(govde, Encoding.UTF8, "application/json") });

    public static readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> Basarili =
        Kod(HttpStatusCode.OK, "{\"id\":\"abc123\",\"time\":1,\"event\":\"message\",\"topic\":\"t\"}");
}

public sealed class NtfyKanaliTestleri
{
    private static readonly HariciBildirimIsi Is = new(1, "uyum-test-konusu-1", "Uyum Eşleştirme", "Yeni bir eşleşme isteğin var.");

    private static (NtfyKanali Kanal, SahteHttp Http) Kur(string anahtar = "", int zamanAsimi = 5, params Func<HttpRequestMessage, Task<HttpResponseMessage>>[] yanitlar)
    {
        var http = new SahteHttp(yanitlar);
        var ayar = new BildirimAyarlari { Anahtar = anahtar, ZamanAsimiSaniye = zamanAsimi, BeklemeMilisaniye = 0, AzamiDeneme = 3 };
        var kanal = new NtfyKanali(new HttpClient(http) { BaseAddress = new Uri("https://ntfy.test/") }, ayar, NullLogger<NtfyKanali>.Instance);
        return (kanal, http);
    }

    [Fact]
    public async Task Basarili_gonderimde_json_govde_ve_anahtar_gonderilir()
    {
        var (kanal, http) = Kur("tk_gizli", yanitlar: SahteHttp.Basarili);
        await kanal.Gonder(Is, CancellationToken.None);

        var (istek, govde) = Assert.Single(http.Istekler);
        Assert.Equal(HttpMethod.Post, istek.Method);
        Assert.Equal("Bearer", istek.Headers.Authorization?.Scheme);
        Assert.Equal("tk_gizli", istek.Headers.Authorization?.Parameter);
        using var json = JsonDocument.Parse(govde);
        Assert.Equal(Is.Konu, json.RootElement.GetProperty("topic").GetString());
        Assert.Equal(Is.Metin, json.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Anahtar_yoksa_authorization_basligi_gonderilmez()
    {
        var (kanal, http) = Kur(yanitlar: SahteHttp.Basarili);
        await kanal.Gonder(Is, CancellationToken.None);
        Assert.Null(http.Istekler[0].Istek.Headers.Authorization);
    }

    [Fact]
    public async Task Gecici_hata_yeniden_denenir()
    {
        var (kanal, http) = Kur(yanitlar: [SahteHttp.Kod(HttpStatusCode.ServiceUnavailable), SahteHttp.Kod(HttpStatusCode.TooManyRequests), SahteHttp.Basarili]);
        await kanal.Gonder(Is, CancellationToken.None);
        Assert.Equal(3, http.Istekler.Count);
    }

    [Fact]
    public async Task Yetki_hatasi_yeniden_denenmez()
    {
        var (kanal, http) = Kur(yanitlar: SahteHttp.Kod(HttpStatusCode.Unauthorized));
        var h = await Assert.ThrowsAsync<BildirimGonderimHatasi>(() => kanal.Gonder(Is, CancellationToken.None));
        Assert.True(h.Kalici);
        Assert.Contains("401", h.Message);
        Assert.Single(http.Istekler);
    }

    [Fact]
    public async Task Surekli_5xx_tum_denemelerden_sonra_hata_verir()
    {
        var (kanal, http) = Kur(yanitlar: SahteHttp.Kod(HttpStatusCode.BadGateway));
        var h = await Assert.ThrowsAsync<BildirimGonderimHatasi>(() => kanal.Gonder(Is, CancellationToken.None));
        Assert.False(h.Kalici);
        Assert.Equal(3, http.Istekler.Count);
    }

    [Fact]
    public async Task Zaman_asimi_yakalanir()
    {
        var (kanal, http) = Kur(zamanAsimi: 1, yanitlar: async _ =>
        {
            await Task.Delay(TimeSpan.FromSeconds(3));
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var h = await Assert.ThrowsAsync<BildirimGonderimHatasi>(() => kanal.Gonder(Is, CancellationToken.None));
        Assert.Contains("zaman aşımı", h.Message);
    }

    [Fact]
    public async Task Baglanti_hatasi_yakalanir()
    {
        var (kanal, _) = Kur(yanitlar: _ => throw new HttpRequestException("ağ yok"));
        var h = await Assert.ThrowsAsync<BildirimGonderimHatasi>(() => kanal.Gonder(Is, CancellationToken.None));
        Assert.Contains("bağlanılamadı", h.Message);
    }

    [Fact]
    public async Task Beklenmeyen_yanit_govdesi_basarisiz_sayilir()
    {
        var (kanal, _) = Kur(yanitlar: SahteHttp.Kod(HttpStatusCode.OK, "<html>bakımda</html>"));
        await Assert.ThrowsAsync<BildirimGonderimHatasi>(() => kanal.Gonder(Is, CancellationToken.None));
    }
}
