using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.EntityFrameworkCore;
using UyumEslestirme.Business;
using UyumEslestirme.Data;
using UyumEslestirme.Web;
using UyumEslestirme.Web.Istemci;

// Web arayuzu iki sekilde calisir:
// 1) API sunucusundan acildiginda (dotnet run --project src/UyumEslestirme.Api): kayit ve giris
//    sunucudaki SQLite veritabanina yazilir, veriler kalicidir, herkes ayni veriyi gorur.
// 2) GitHub Pages'te (sunucu yok): demo; is katmani tarayicinin belleginde calisir.
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");

var saat = TimeProvider.System;
var http = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };

if (await ApiVarMi(http))
{
    builder.Services.AddSingleton<IUygulama>(new ApiUygulama(http, saat));
}
else
{
    ParolaHasleyici.Tekrar = 2_000;
    var db = new UygulamaDbContext(new DbContextOptionsBuilder<UygulamaDbContext>()
        .UseInMemoryDatabase("uyum")
        .Options);
    var hash = ParolaHasleyici.Hashle(Demo.Parola);
    VeriTohumlayici.SorulariTohumla(db);
    VeriTohumlayici.Tohumla(db, hash, hash, saat.GetUtcNow().UtcDateTime);
    builder.Services.AddSingleton<IUygulama>(new YerelUygulama(new Servisler(db, saat)));
}

await builder.Build().RunAsync();

// Sayfayi sunan adreste API varsa sunucu modunda calisilir.
static async Task<bool> ApiVarMi(HttpClient http)
{
    try
    {
        using var sure = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var saglik = await http.GetFromJsonAsync<SaglikYaniti>("api/saglik", sure.Token);
        return saglik?.Durum == "ok";
    }
    catch (Exception)
    {
        return false;
    }
}

internal sealed record SaglikYaniti(string Durum, string Surum);
