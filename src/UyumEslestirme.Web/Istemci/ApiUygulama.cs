using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using UyumEslestirme.Business;
using UyumEslestirme.Domain;

namespace UyumEslestirme.Web.Istemci;

// Sunucudaki API'nin istemcisi. Giriste alinan JWT bellekte tutulur ve her istekte gonderilir;
// parola tarayicida saklanmaz. Sunucunun hata mesajlari is katmanindaki ayni hata turlerine cevrilir,
// boylece sayfalar iki modda da ayni sekilde calisir.
public sealed class ApiUygulama(HttpClient http, TimeProvider saat) : IUygulama
{
    private string? _token;
    private DateTimeOffset _bitis;

    public bool GercekVeritabani => true;
    public bool Acik => _token is not null && saat.GetUtcNow() < _bitis;
    public string AdSoyad { get; private set; } = "";
    public Rol Rol { get; private set; }

    public async Task GirisYap(string eposta, string parola)
    {
        var giris = await Gonder<GirisYaniti>(HttpMethod.Post, "api/kimlik/giris", new GirisIstegi(eposta, parola));
        _token = giris.Token;
        _bitis = giris.Bitis;
        AdSoyad = giris.AdSoyad;
        Rol = giris.Rol;
    }

    public Task KayitOl(string adSoyad, string eposta, string parola, string parolaTekrar) =>
        Gonder(HttpMethod.Post, "api/kimlik/kayit", new KayitIstegi(adSoyad, eposta, parola, parolaTekrar));

    public async Task CikisYap()
    {
        try
        {
            if (Acik)
                await Gonder(HttpMethod.Post, "api/kimlik/cikis");
        }
        catch (Exception h) when (h is IsKuraliHatasi or YetkisizErisimHatasi)
        {
            // Sunucuya ulasilamasa da yerel oturum kapanir.
        }
        finally
        {
            Temizle();
        }
    }

    public Task<ProfilBilgisi?> Profil() => Al<ProfilBilgisi?>("api/profil");
    public Task ProfilKaydet(ProfilGirdisi girdi) => Gonder(HttpMethod.Put, "api/profil", girdi);

    public Task<List<SoruBilgisi>> Sorular() => Al<List<SoruBilgisi>>("api/anket/sorular")!;
    public Task<List<CevapGirdisi>> Cevaplarim() => Al<List<CevapGirdisi>>("api/anket/cevaplarim")!;
    public Task CevaplariKaydet(List<CevapGirdisi> cevaplar) => Gonder(HttpMethod.Put, "api/anket/cevaplarim", cevaplar);
    public Task<List<KategoriOzeti>> TercihOzeti() => Al<List<KategoriOzeti>>("api/anket/ozet")!;

    public Task<List<AdayOzeti>> Adaylar() => Al<List<AdayOzeti>>("api/adaylar")!;
    public Task IstekGonder(int aliciId) => Gonder(HttpMethod.Post, "api/eslesmeler", new IstekGonderIstegi(aliciId));
    public Task<EslesmeListeleri> Eslesmeler() => Al<EslesmeListeleri>("api/eslesmeler")!;
    public Task Yanitla(int eslesmeId, bool kabul) => Gonder(HttpMethod.Post, $"api/eslesmeler/{eslesmeId}/{(kabul ? "kabul" : "red")}");
    public Task GeriCek(int eslesmeId) => Gonder(HttpMethod.Delete, $"api/eslesmeler/{eslesmeId}");

    public Task<List<MesajOzeti>> Konusma(int kisiId) => Al<List<MesajOzeti>>($"api/mesajlar/{kisiId}")!;
    public Task MesajGonder(int kisiId, string icerik) => Gonder(HttpMethod.Post, "api/mesajlar", new MesajGonderIstegi(kisiId, icerik));

    public Task<List<BildirimOzeti>> Bildirimler() => Al<List<BildirimOzeti>>("api/bildirimler")!;
    public async Task<int> OkunmamisBildirim() => (await Al<OkunmamisYaniti>("api/bildirimler/okunmamis"))!.Sayi;
    public Task TumunuOkundu() => Gonder(HttpMethod.Post, "api/bildirimler/okundu");
    public Task<BildirimTercihleri> BildirimTercihleri() => Al<BildirimTercihleri>("api/bildirimler/tercihler")!;
    public Task BildirimTercihleriKaydet(BildirimTercihleri tercihler) => Gonder(HttpMethod.Put, "api/bildirimler/tercihler", tercihler);
    public Task DenemeBildirimi() => Gonder(HttpMethod.Post, "api/bildirimler/deneme");

    public Task<OneriSonucu> Oneriler() => Al<OneriSonucu>("api/oneriler")!;

    public Task<List<KullaniciOzeti>> Kullanicilar() => Al<List<KullaniciOzeti>>("api/yonetim/kullanicilar")!;

    private sealed record OkunmamisYaniti(int Sayi);

    // ---- HTTP yardimcilari ----

    private async Task<T?> Al<T>(string adres)
    {
        using var yanit = await Istek(new HttpRequestMessage(HttpMethod.Get, adres));
        // 204: kayit yok (ornek: profil henuz olusturulmadi).
        if (yanit.StatusCode == HttpStatusCode.NoContent)
            return default;
        return await yanit.Content.ReadFromJsonAsync<T>();
    }

    private async Task Gonder(HttpMethod yontem, string adres, object? govde = null)
    {
        using var _ = await Istek(Mesaj(yontem, adres, govde));
    }

    private async Task<T> Gonder<T>(HttpMethod yontem, string adres, object govde)
    {
        using var yanit = await Istek(Mesaj(yontem, adres, govde));
        return (await yanit.Content.ReadFromJsonAsync<T>())!;
    }

    private static HttpRequestMessage Mesaj(HttpMethod yontem, string adres, object? govde) =>
        new(yontem, adres) { Content = govde is null ? null : JsonContent.Create(govde, govde.GetType()) };

    private async Task<HttpResponseMessage> Istek(HttpRequestMessage istek)
    {
        if (_token is not null)
            istek.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);

        HttpResponseMessage yanit;
        try
        {
            yanit = await http.SendAsync(istek);
        }
        catch (HttpRequestException)
        {
            throw new IsKuraliHatasi("Sunucuya ulaşılamadı. İnternet bağlantınızı kontrol edip tekrar deneyin.");
        }
        finally
        {
            istek.Dispose();
        }

        if (yanit.IsSuccessStatusCode)
            return yanit;

        using (yanit)
        {
            var mesaj = "Beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.";
            try
            {
                if (await yanit.Content.ReadFromJsonAsync<HataYaniti>() is { Mesaj.Length: > 0 } h)
                    mesaj = h.Mesaj;
            }
            catch (Exception)
            {
                // Govde JSON degilse genel mesaj kalir.
            }

            switch (yanit.StatusCode)
            {
                case HttpStatusCode.Unauthorized:
                    Temizle();
                    throw new YetkisizErisimHatasi("Oturum süreniz doldu. Lütfen tekrar giriş yapın.");
                case HttpStatusCode.Forbidden:
                    throw new YetkisizErisimHatasi(mesaj);
                default:
                    throw new IsKuraliHatasi(mesaj);
            }
        }
    }

    private void Temizle()
    {
        _token = null;
        AdSoyad = "";
        Rol = Rol.Ogrenci;
    }
}
