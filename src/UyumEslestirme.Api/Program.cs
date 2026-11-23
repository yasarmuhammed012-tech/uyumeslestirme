using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using UyumEslestirme.Api;
using UyumEslestirme.Api.Kimlik;
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
VeritabaniHazirlayici.KlasoruOlustur(baglanti);
builder.Services.AddDbContext<UygulamaDbContext>(o => o.UseSqlite(baglanti));
builder.Services.AddSingleton(TimeProvider.System);

// ---- Kimlik dogrulama: JWT ----
var jwt = ayar.GetSection(JwtAyarlari.Bolum).Get<JwtAyarlari>() ?? new JwtAyarlari();
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

// Her istek icin yeni Servisler: oturum, dogrulanmis token'daki kullanici kimligiyle acilir.
// Istek govdesinde gelen bir kimlik hicbir yerde kullanilmaz.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped(sp =>
{
    var servisler = new Servisler(sp.GetRequiredService<UygulamaDbContext>(), sp.GetRequiredService<TimeProvider>());
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

app.Run();

// WebApplicationFactory ile entegrasyon testleri icin.
public partial class Program;
