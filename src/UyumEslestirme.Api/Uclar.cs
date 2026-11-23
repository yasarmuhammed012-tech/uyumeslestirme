using UyumEslestirme.Api.Kimlik;
using UyumEslestirme.Business;
using UyumEslestirme.Domain;

namespace UyumEslestirme.Api;

// Uc noktalar ince bir katmandir: istegi alir, is katmanindaki servisi cagirir, sonucu dondurur.
// Kural kontrolleri (gizlilik, istek siniri, kilit...) servislerin icindedir; burada tekrar yazilmaz.
// Kullanici kimligi hicbir ucta parametre olarak alinmaz; Servisler token'dan acilan oturumu kullanir.
public static class Uclar
{
    public const string GirisSiniri = "giris";

    public static void Esle(WebApplication app)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/saglik", () => Results.Ok(new { durum = "ok", surum = UygulamaSurumu.Metin }));

        // ---- Kimlik ----
        var kimlik = api.MapGroup("/kimlik");

        kimlik.MapPost("/kayit", (KayitIstegi i, Servisler s) =>
        {
            s.Kimlik.KayitOl(i.AdSoyad, i.Eposta, i.Parola, i.ParolaTekrar);
            return Results.NoContent();
        }).RequireRateLimiting(GirisSiniri);

        kimlik.MapPost("/giris", (GirisIstegi i, Servisler s, TokenServisi token) =>
        {
            s.Kimlik.GirisYap(i.Eposta, i.Parola);
            return Results.Ok(token.Uret(s.Oturum));
        }).RequireRateLimiting(GirisSiniri);

        kimlik.MapPost("/cikis", (HttpContext http, IptalListesi iptaller) =>
        {
            var id = http.User.FindFirst(Talepler.TokenId)?.Value;
            var bitis = long.TryParse(http.User.FindFirst("exp")?.Value, out var exp)
                ? DateTimeOffset.FromUnixTimeSeconds(exp)
                : DateTimeOffset.UtcNow + TokenServisi.Sure;
            if (id is not null)
                iptaller.Ekle(id, bitis);
            return Results.NoContent();
        }).RequireAuthorization();

        // ---- Giris yapmis her kullanici ----
        var oturum = api.MapGroup("").RequireAuthorization();

        oturum.MapGet("/profil", (Servisler s) =>
            s.Profil.BenimProfilim() is { } p ? Results.Ok(ProfilBilgisi.Olustur(p)) : Results.NoContent());

        oturum.MapPut("/profil", (ProfilGirdisi girdi, Servisler s) =>
        {
            s.Profil.Kaydet(girdi);
            return Results.NoContent();
        });

        oturum.MapPut("/profil/gorunurluk", (GorunurlukIstegi i, Servisler s) =>
        {
            s.Profil.GorunurlukAyarla(i.Gorunur);
            return Results.NoContent();
        });

        oturum.MapGet("/anket/sorular", (Servisler s) => s.Anket.Sorular().Select(SoruBilgisi.Olustur).ToList());

        oturum.MapGet("/anket/cevaplarim", (Servisler s) =>
            s.Anket.BenimCevaplarim().Select(c => new CevapGirdisi(c.SoruId, c.Deger, c.Agirlik)).ToList());

        oturum.MapPut("/anket/cevaplarim", (List<CevapGirdisi> cevaplar, Servisler s) =>
        {
            s.Anket.CevaplariKaydet(cevaplar);
            return Results.NoContent();
        });

        oturum.MapGet("/anket/ozet", (Servisler s) => s.Anket.TercihOzeti());

        oturum.MapGet("/adaylar", (Servisler s) => s.Adaylar.Adaylar());

        oturum.MapGet("/eslesmeler", (Servisler s) => s.Eslesme.Listele());

        oturum.MapPost("/eslesmeler", (IstekGonderIstegi i, Servisler s) =>
        {
            s.Eslesme.IstekGonder(i.AliciId);
            return Results.NoContent();
        });

        oturum.MapPost("/eslesmeler/{id:int}/kabul", (int id, Servisler s) =>
        {
            s.Eslesme.Yanitla(id, kabul: true);
            return Results.NoContent();
        });

        oturum.MapPost("/eslesmeler/{id:int}/red", (int id, Servisler s) =>
        {
            s.Eslesme.Yanitla(id, kabul: false);
            return Results.NoContent();
        });

        oturum.MapDelete("/eslesmeler/{id:int}", (int id, Servisler s) =>
        {
            s.Eslesme.GeriCek(id);
            return Results.NoContent();
        });

        // ---- Yalnizca yonetici ----
        // Rol hem burada (403) hem YonetimServisi icinde denetlenir.
        var yonetim = api.MapGroup("/yonetim").RequireAuthorization(p => p.RequireRole(nameof(Rol.Yonetici)));

        yonetim.MapGet("/kullanicilar", (Servisler s) => s.Yonetim.KullanicilariListele());
    }
}
