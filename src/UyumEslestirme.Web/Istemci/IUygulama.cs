using UyumEslestirme.Business;
using UyumEslestirme.Domain;

namespace UyumEslestirme.Web.Istemci;

// Sayfalarin kullandigi tek arayuz. Iki uygulamasi var:
// - ApiUygulama: sunucudaki API'ye baglanir; kayit ve giris ortak SQLite veritabanina yazilir (gercek kullanim).
// - YerelUygulama: GitHub Pages demosu; is katmanini tarayicinin belleginde calistirir.
public interface IUygulama
{
    bool GercekVeritabani { get; }
    bool Acik { get; }
    string AdSoyad { get; }
    Rol Rol { get; }

    Task GirisYap(string eposta, string parola);
    Task KayitOl(string adSoyad, string eposta, string parola, string parolaTekrar);
    Task CikisYap();

    Task<ProfilBilgisi?> Profil();
    Task ProfilKaydet(ProfilGirdisi girdi);

    Task<List<SoruBilgisi>> Sorular();
    Task<List<CevapGirdisi>> Cevaplarim();
    Task CevaplariKaydet(List<CevapGirdisi> cevaplar);
    Task<List<KategoriOzeti>> TercihOzeti();

    Task<List<AdayOzeti>> Adaylar();
    Task IstekGonder(int aliciId);
    Task<EslesmeListeleri> Eslesmeler();
    Task Yanitla(int eslesmeId, bool kabul);
    Task GeriCek(int eslesmeId);

    Task<List<MesajOzeti>> Konusma(int kisiId);
    Task MesajGonder(int kisiId, string icerik);

    Task<List<BildirimOzeti>> Bildirimler();
    Task<int> OkunmamisBildirim();
    Task TumunuOkundu();
    Task<BildirimTercihleri> BildirimTercihleri();
    Task BildirimTercihleriKaydet(BildirimTercihleri tercihler);
    Task DenemeBildirimi();

    Task<OneriSonucu> Oneriler();

    Task<List<KullaniciOzeti>> Kullanicilar();
}

public sealed class YerelUygulama(Servisler s) : IUygulama
{
    public bool GercekVeritabani => false;
    public bool Acik => s.Oturum.Acik;
    public string AdSoyad => s.Oturum.AdSoyad;
    public Rol Rol => s.Oturum.Rol;

    public Task GirisYap(string eposta, string parola) => Yap(() => s.Kimlik.GirisYap(eposta, parola));
    public Task KayitOl(string adSoyad, string eposta, string parola, string parolaTekrar) => Yap(() => s.Kimlik.KayitOl(adSoyad, eposta, parola, parolaTekrar));
    public Task CikisYap() => Yap(s.Kimlik.CikisYap);

    public Task<ProfilBilgisi?> Profil() => Al(() => s.Profil.BenimProfilim() is { } p ? ProfilBilgisi.Olustur(p) : null);
    public Task ProfilKaydet(ProfilGirdisi girdi) => Yap(() => s.Profil.Kaydet(girdi));

    public Task<List<SoruBilgisi>> Sorular() => Al(() => s.Anket.Sorular().Select(SoruBilgisi.Olustur).ToList());
    public Task<List<CevapGirdisi>> Cevaplarim() => Al(() => s.Anket.BenimCevaplarim().Select(c => new CevapGirdisi(c.SoruId, c.Deger, c.Agirlik)).ToList());
    public Task CevaplariKaydet(List<CevapGirdisi> cevaplar) => Yap(() => s.Anket.CevaplariKaydet(cevaplar));
    public Task<List<KategoriOzeti>> TercihOzeti() => Al(s.Anket.TercihOzeti);

    public Task<List<AdayOzeti>> Adaylar() => Al(s.Adaylar.Adaylar);
    public Task IstekGonder(int aliciId) => Yap(() => s.Eslesme.IstekGonder(aliciId));
    public Task<EslesmeListeleri> Eslesmeler() => Al(s.Eslesme.Listele);
    public Task Yanitla(int eslesmeId, bool kabul) => Yap(() => s.Eslesme.Yanitla(eslesmeId, kabul));
    public Task GeriCek(int eslesmeId) => Yap(() => s.Eslesme.GeriCek(eslesmeId));

    public Task<List<MesajOzeti>> Konusma(int kisiId) => Al(() => s.Mesaj.Konusma(kisiId));
    public Task MesajGonder(int kisiId, string icerik) => Yap(() => s.Mesaj.Gonder(kisiId, icerik));

    public Task<List<BildirimOzeti>> Bildirimler() => Al(s.Bildirim.Listele);
    public Task<int> OkunmamisBildirim() => Al(s.Bildirim.OkunmamisSayisi);
    public Task TumunuOkundu() => Yap(s.Bildirim.TumunuOkundu);
    public Task<BildirimTercihleri> BildirimTercihleri() => Al(s.Bildirim.Tercihlerim);
    public Task BildirimTercihleriKaydet(BildirimTercihleri tercihler) => Yap(() => s.Bildirim.TercihleriKaydet(tercihler));
    public Task DenemeBildirimi() => Yap(s.Bildirim.DenemeGonder);

    public Task<OneriSonucu> Oneriler() => s.Oneri.Oner();

    public Task<List<KullaniciOzeti>> Kullanicilar() => Al(s.Yonetim.KullanicilariListele);

    private static Task Yap(Action islem)
    {
        islem();
        return Task.CompletedTask;
    }

    private static Task<T> Al<T>(Func<T> islem) => Task.FromResult(islem());
}
