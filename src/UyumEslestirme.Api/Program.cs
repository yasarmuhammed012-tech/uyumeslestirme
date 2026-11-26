using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Anthropic;
using UyumEslestirme.Api;
using UyumEslestirme.Api.Bildirim;
using UyumEslestirme.Api.Kimlik;
using UyumEslestirme.Api.YapayZeka;
using UyumEslestirme.Business;
using UyumEslestirme.Data;
using UyumEslestirme.Domain;

// Ortak sunucu: farkli bilgisayarlardaki web istemcileri ayni veritabanini bu API uzerinden kullanir.
// Is kurallari Business katmanindadir; API yalnizca HTTP, kimlik dogrulama ve veritabani baglantisini ekler.
var builder = WebApplication.CreateBuilder(args);
var ayar = builder.Configuration;

// ---- Veritabani: ortak SQLite dosyasi, yolu ayardan gelir (ConnectionStrings__Uyum) ----
var baglanti = ayar.GetConnectionString("Uyum")
    ?? throw new InvalidOperationException("ConnectionStrings:Uyum ayarı yok.");
baglanti = VeritabaniHazirlayici.Hazirla(baglanti, builder.Environment.ContentRootPath);
builder.Services.AddDbContext<UygulamaDbContext>(o => o.UseSqlite(baglanti));
builder.Services.AddSingleton(TimeProvider.System);

// ---- Kimlik dogrulama: JWT ----
var jwt = ayar.GetSection(JwtAyarlari.Bolum).Get<JwtAyarlari>() ?? new JwtAyarlari();
if (string.IsNullOrEmpty(jwt.Anahtar))
{
    // Anahtar ayarda yoksa sunucu ilk acilista rastgele uretir ve veritabaninin yaninda saklar
    // (depoya ve koda girmez; yeniden baslatmada ayni anahtar kullanilir, oturumlar gecerli kalir).
    jwt.Anahtar = JwtAyarlari.DosyadanVeyaYeni(Path.Combine(
        Path.GetDirectoryName(new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(baglanti).DataSource)!, "jwt-anahtari.txt"));
}
var anahtar = jwt.GuvenlikAnahtari();
builder.Services.AddSingleton(jwt);
builder.Services.AddSingleton<TokenServisi>();
builder.Services.AddSingleton<IptalListesi>();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Yayinci,
            ValidAudience = jwt.Hedef,
            IssuerSigningKey = anahtar,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            NameClaimType = Talepler.AdSoyad,
            RoleClaimType = Talepler.Rol,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
        o.Events = new JwtBearerEvents
        {
            OnTokenValidated = ctx =>
            {
                var iptaller = ctx.HttpContext.RequestServices.GetRequiredService<IptalListesi>();
                if (iptaller.IptalMi(ctx.Principal?.FindFirst(Talepler.TokenId)?.Value))
                    ctx.Fail("Oturum kapatıldı.");
                return Task.CompletedTask;
            },
        };
    });
builder.Services.AddAuthorization();

// ---- Hafta 9: telefon bildirimi (ntfy REST API) ----
var bildirimAyar = ayar.GetSection(BildirimAyarlari.Bolum).Get<BildirimAyarlari>() ?? new BildirimAyarlari();
builder.Services.AddSingleton(bildirimAyar);
builder.Services.AddSingleton<BildirimKuyrugu>();
builder.Services.AddHttpClient<IHariciBildirimKanali, NtfyKanali>(http =>
{
    http.BaseAddress = new Uri(bildirimAyar.Adres);
    // Deneme basina zaman asimi NtfyKanali icinde yonetilir; burasi yalnizca ust sinirdir.
    http.Timeout = TimeSpan.FromSeconds(bildirimAyar.ZamanAsimiSaniye * bildirimAyar.AzamiDeneme + 30);
});
builder.Services.AddHostedService<BildirimGondericisi>();

// ---- Hafta 10: aciklamali oneri icin Claude. Anahtar yoksa oneriler kural tabanli yazilir. ----
var yzAyar = ayar.GetSection(YapayZekaAyarlari.Bolum).Get<YapayZekaAyarlari>() ?? new YapayZekaAyarlari();
if (string.IsNullOrEmpty(yzAyar.Anahtar))
    yzAyar.Anahtar = ayar["ANTHROPIC_API_KEY"] ?? "";
builder.Services.AddSingleton(yzAyar);
if (!string.IsNullOrEmpty(yzAyar.Anahtar))
{
    builder.Services.AddSingleton(new AnthropicClient { ApiKey = yzAyar.Anahtar, Timeout = OneriServisi.VarsayilanZamanAsimi });
    builder.Services.AddSingleton<IYapayZekaIstemcisi, ClaudeIstemcisi>();
}

// Her istek icin yeni Servisler: oturum, dogrulanmis token'daki kullanici kimligiyle acilir.
// Istek govdesinde gelen bir kimlik hicbir yerde kullanilmaz.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped(sp =>
{
    var servisler = new Servisler(
        sp.GetRequiredService<UygulamaDbContext>(),
        sp.GetRequiredService<TimeProvider>(),
        sp.GetRequiredService<BildirimKuyrugu>(),
        sp.GetService<IYapayZekaIstemcisi>());
    var kullanici = sp.GetRequiredService<IHttpContextAccessor>().HttpContext?.User;
    if (kullanici is not null && Talepler.Oku(kullanici) is { } k)
        servisler.Oturum.Ac(k.Id, k.AdSoyad, k.Rol);
    return servisler;
});

// ---- JSON: eksik veya null alanlar 400 doner, servislere null ulasmaz ----
builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.RespectNullableAnnotations = true;
    o.SerializerOptions.RespectRequiredConstructorParameters = true;
});

// ---- CORS: yalnizca ayarda yazili web adreslerine acik ----
var kaynaklar = ayar.GetSection("Cors:IzinliKaynaklar").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(kaynaklar)
    .WithHeaders("Authorization", "Content-Type")
    .WithMethods("GET", "POST", "PUT", "DELETE")));

// ---- Giris/kayit icin IP basina hiz siniri (hesap kilidine ek olarak) ----
var dakikadaGiris = ayar.GetValue("HizSiniri:DakikadaGiris", 10);
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.OnRejected = (ctx, iptal) => new ValueTask(ctx.HttpContext.Response.WriteAsJsonAsync(
        new HataYaniti("Çok fazla deneme yapıldı. Lütfen bir dakika sonra tekrar deneyin."), iptal));
    o.AddPolicy(Uclar.GirisSiniri, http => RateLimitPartition.GetFixedWindowLimiter(
        http.Connection.RemoteIpAddress?.ToString() ?? "bilinmiyor",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = dakikadaGiris, Window = TimeSpan.FromMinutes(1) }));
    // Yapay zeka cagrisi ucretli ve yavas: kullanici basina dakikada sinirli.
    o.AddPolicy(Uclar.OneriSiniri, http => RateLimitPartition.GetFixedWindowLimiter(
        http.User.FindFirst(Talepler.KullaniciId)?.Value ?? http.Connection.RemoteIpAddress?.ToString() ?? "bilinmiyor",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = ayar.GetValue("HizSiniri:DakikadaOneri", 5), Window = TimeSpan.FromMinutes(1) }));
});

// Barindirma ortaminda istek bir vekil sunucudan gelir; istemci IP'si son X-Forwarded-For degeridir.
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

var app = builder.Build();

VeritabaniHazirlayici.Hazirla(app.Services);

app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment())
    app.UseHsts();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseMiddleware<HataKarsilayici>();

Uclar.Esle(app);

// Web arayuzu (Blazor WebAssembly) ayni sunucudan yayinlanir; tarayici API'ye ayni adresten baglanir.
// MapStaticAssets, "_framework/blazor.webassembly.js" gibi sabit adlari parmak izli dosyalara baglar.
app.MapStaticAssets();
app.MapFallback(WebArayuzu.IndexGonder);

app.Run();

// index.html'deki "#[.{fingerprint}]" yer tutucusu yalnizca tek basina yayinda (GitHub Pages) doldurulur.
// Sunucu sayfayi gonderirken yer tutucuyu kaldirir; sabit ad MapStaticAssets ile cozulur.
static class WebArayuzu
{
    private static string? _sayfa;

    public static async Task IndexGonder(HttpContext http)
    {
        if (http.Request.Path.StartsWithSegments("/api"))
        {
            http.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }
        if (_sayfa is null)
        {
            var dosya = http.RequestServices.GetRequiredService<IWebHostEnvironment>().WebRootFileProvider.GetFileInfo("index.html");
            if (!dosya.Exists)
            {
                http.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }
            using var okuyucu = new StreamReader(dosya.CreateReadStream());
            _sayfa = (await okuyucu.ReadToEndAsync()).Replace("#[.{fingerprint}]", "");
        }
        http.Response.ContentType = "text/html; charset=utf-8";
        http.Response.Headers.CacheControl = "no-cache";
        await http.Response.WriteAsync(_sayfa);
    }
}

// WebApplicationFactory ile entegrasyon testleri icin.
public partial class Program;
