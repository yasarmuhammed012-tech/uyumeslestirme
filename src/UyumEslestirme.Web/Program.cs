using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.EntityFrameworkCore;
using UyumEslestirme.Business;
using UyumEslestirme.Data;
using UyumEslestirme.Web;

// Tarayici surumu: veriler tarayicinin belleginde tutulur, sayfa yenilenince demo verilere doner.
// Is kurallari ve veri erisimi masaustu surumuyle ayni katmanlardan gelir.
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");

ParolaHasleyici.Tekrar = 2_000;

var db = new UygulamaDbContext(new DbContextOptionsBuilder<UygulamaDbContext>()
    .UseInMemoryDatabase("uyum")
    .Options);
var saat = TimeProvider.System;
var hash = ParolaHasleyici.Hashle(Demo.Parola);
VeriTohumlayici.SorulariTohumla(db);
VeriTohumlayici.Tohumla(db, hash, hash, saat.GetUtcNow().UtcDateTime);

builder.Services.AddSingleton(new Servisler(db, saat));

await builder.Build().RunAsync();
