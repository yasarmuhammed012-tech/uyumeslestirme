using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.Sqlite;
using UyumEslestirme.Business;

namespace UyumEslestirme.Api.Tests;

// API'yi bellekte gercek ayarlariyla calistirir. Her fabrika gecici bir SQLite dosyasi kullanir;
// ayni dosyayla ikinci bir fabrika acmak, sunucunun yeniden baslamasini taklit eder.
public sealed class ApiFabrikasi(string? dbDosyasi = null) : WebApplicationFactory<Program>
{
    public const string YoneticiEposta = "yonetici@test.local";
    public const string YoneticiParola = "Yonetici-Parola-123";

    // Testler arasinda paylasilan anahtar: kalicilik testinde ikinci sunucu da ayni anahtari kullanir.
    private static readonly string JwtAnahtari = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

    public string DbDosyasi { get; } = dbDosyasi ?? Path.Combine(Path.GetTempPath(), $"uyum-api-test-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        builder.UseSetting("ConnectionStrings:Uyum", $"Data Source={DbDosyasi};Pooling=False");
        builder.UseSetting("Jwt:Anahtar", JwtAnahtari);
        builder.UseSetting("HizSiniri:DakikadaGiris", "100000");
        builder.UseSetting("Tohum:DemoVerisi", "false");
        builder.UseSetting("Yonetici:Eposta", YoneticiEposta);
        builder.UseSetting("Yonetici:Parola", YoneticiParola);
        builder.UseSetting("HizSiniri:DakikadaOneri", "100000");
        builder.UseSetting("ANTHROPIC_API_KEY", "");
        builder.ConfigureTestServices(s => SahteServisler?.Invoke(s));
    }

    // Harici servisler (ntfy, Claude) yerine sahteleri koymak icin.
    public Action<IServiceCollection>? SahteServisler { get; init; }

    public Istemci Istemci() => new(CreateClient());

    public void DosyayiSil()
    {
        SqliteConnection.ClearAllPools();
        File.Delete(DbDosyasi);
    }
}

// Bir tarayici sekmesi gibi davranir: girisle aldigi token'i sonraki isteklerde gonderir.
public sealed class Istemci(HttpClient http)
{
    public HttpClient Http { get; } = http;

    public async Task KayitOl(string eposta, string adSoyad = "Test Öğrenci", string parola = "Parola123")
    {
        var y = await Http.PostAsJsonAsync("/api/kimlik/kayit", new KayitIstegi(adSoyad, eposta, parola, parola));
        await Basarili(y);
    }

    public async Task<HttpResponseMessage> GirisDene(string eposta, string parola) =>
        await Http.PostAsJsonAsync("/api/kimlik/giris", new GirisIstegi(eposta, parola));

    public async Task<GirisYaniti> GirisYap(string eposta, string parola = "Parola123")
    {
        var y = await GirisDene(eposta, parola);
        await Basarili(y);
        var giris = (await y.Content.ReadFromJsonAsync<GirisYaniti>())!;
        Http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", giris.Token);
        return giris;
    }

    // Kayit + giris + profil + anket: Arkadas bul listesinde gorunmeye hazir ogrenci.
    public async Task HazirOgrenci(string eposta, string adSoyad, int deger = 3, bool gorunur = true)
    {
        await KayitOl(eposta, adSoyad);
        await GirisYap(eposta);
        await Basarili(await Http.PutAsJsonAsync("/api/profil", new ProfilGirdisi("Bilgisayar Programcılığı", "Bingöl", 2004, null, gorunur)));
        var sorular = await Al<List<SoruBilgisi>>("/api/anket/sorular");
        await Basarili(await Http.PutAsJsonAsync("/api/anket/cevaplarim",
            sorular.Select(s => new CevapGirdisi(s.Id, deger, 3)).ToList()));
    }

    public async Task<T> Al<T>(string adres)
    {
        var y = await Http.GetAsync(adres);
        await Basarili(y);
        return (await y.Content.ReadFromJsonAsync<T>())!;
    }

    public static async Task Basarili(HttpResponseMessage y)
    {
        if (!y.IsSuccessStatusCode)
            Assert.Fail($"{(int)y.StatusCode} {y.StatusCode}: {await y.Content.ReadAsStringAsync()}");
    }

    public static async Task<string> HataMesaji(HttpResponseMessage y, HttpStatusCode beklenen)
    {
        Assert.Equal(beklenen, y.StatusCode);
        return (await y.Content.ReadFromJsonAsync<HataYaniti>())!.Mesaj;
    }
}
