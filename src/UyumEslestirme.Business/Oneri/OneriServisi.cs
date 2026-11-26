using System.Globalization;
using UyumEslestirme.Domain;

namespace UyumEslestirme.Business;

public enum OneriKaynagi
{
    YapayZeka = 0,
    KuralTabanli = 1,
}

public sealed record UyumOnerisi(
    int KullaniciId,
    string AdSoyad,
    string Bolum,
    string Sehir,
    double UyumYuzdesi,
    string Gerekce,
    List<SoruKategorisi> GucluYonler,
    SoruKategorisi? DikkatKonusu,
    string? DikkatNotu);

// Kaynak: gerekceyi kim yazdi. Aciklama: yapay zeka kullanilamadiysa nedeni (kullaniciya seffaflik).
public sealed record OneriSonucu(List<UyumOnerisi> Oneriler, OneriKaynagi Kaynak, string? Aciklama);

// Dil modeli baglantisi. Gercek istemci API projesindedir (anahtar yalnizca sunucuda durur);
// testlerde sahte istemci kullanilir.
public interface IYapayZekaIstemcisi
{
    // Verilen JSON semasina uyan bir JSON metni dondurur; ulasilamazsa YapayZekaHatasi firlatir.
    Task<string> JsonUret(string sistem, string istem, string jsonSemasi, CancellationToken iptal);
}

public sealed class YapayZekaHatasi(string mesaj, Exception? ic = null) : Exception(mesaj, ic);

// Bir adayin skorunu olusturan, veritabanindan hesaplanan gercekler. Yapay zekaya yalnizca bunlar gider.
public sealed record KategoriGercegi(SoruKategorisi Kategori, double Uyum, double BenimOrtalama, double OnunOrtalama, double Onem);

public sealed record AdayGercekleri(string Kod, AdayOzeti Aday, List<KategoriGercegi> Kategoriler)
{
    public KategoriGercegi Kategori(SoruKategorisi k) => Kategoriler.First(x => x.Kategori == k);
}

// 10. hafta: Aciklamali uyum onerisi.
// 1) Kimin onerilecegine skor karar verir (UyumHesaplayici, deterministik).
// 2) Yapay zeka yalnizca bu kisilerin neden uygun oldugunu gercek verilere dayanarak yazar.
// 3) Cikti OneriDogrulayici'dan gecmezse veya servis yanit vermezse kural tabanli gerekce gosterilir.
public sealed class OneriServisi(AdayServisi adaylar, UyumServisi uyum, OturumYoneticisi oturum, IYapayZekaIstemcisi? yapayZeka)
{
    public const int OneriSayisi = 3;
    public static readonly TimeSpan VarsayilanZamanAsimi = TimeSpan.FromSeconds(45);

    public TimeSpan ZamanAsimi { get; init; } = VarsayilanZamanAsimi;

    public async Task<OneriSonucu> Oner(CancellationToken iptal = default)
    {
        oturum.RolGerekli(Rol.Ogrenci);

        // Henuz istek gonderilmemis en yuksek skorlu kisiler; siralama skora gore zaten yapilmistir.
        var secilenler = adaylar.Adaylar().Where(a => a.Durum == AdayDurumu.Yok).Take(OneriSayisi).ToList();
        if (secilenler.Count == 0)
            return new OneriSonucu([], OneriKaynagi.KuralTabanli, "Şu an önerilecek yeni kişi yok.");

        var gercekler = Gercekler(secilenler);

        if (yapayZeka is null)
            return KuralTabanli(gercekler, "Yapay zekâ servisi bu sürümde tanımlı değil; gerekçeler kural tabanlı yazıldı.");

        string json;
        try
        {
            using var sure = CancellationTokenSource.CreateLinkedTokenSource(iptal);
            sure.CancelAfter(ZamanAsimi);
            json = await yapayZeka.JsonUret(OneriIstemi.Sistem, OneriIstemi.Kullanici(gercekler), OneriIstemi.Sema, sure.Token);
        }
        catch (OperationCanceledException) when (!iptal.IsCancellationRequested)
        {
            return KuralTabanli(gercekler, "Yapay zekâ servisi zamanında yanıt vermedi; gerekçeler kural tabanlı yazıldı.");
        }
        catch (YapayZekaHatasi h)
        {
            return KuralTabanli(gercekler, $"Yapay zekâ servisine ulaşılamadı ({h.Message}); gerekçeler kural tabanlı yazıldı.");
        }

        var dogrulama = OneriDogrulayici.Dogrula(json, gercekler);
        if (dogrulama.Hatalar.Count > 0)
        {
            return KuralTabanli(gercekler,
                $"Yapay zekâ yanıtı doğrulamadan geçmedi ({string.Join("; ", dogrulama.Hatalar.Take(2))}); gerekçeler kural tabanlı yazıldı.");
        }

        var oneriler = gercekler.Select(g =>
        {
            var y = dogrulama.Oneriler[g.Kod];
            return Oneri(g, y.Gerekce, y.GucluYonler, y.DikkatKonusu, y.DikkatNotu);
        }).ToList();
        return new OneriSonucu(oneriler, OneriKaynagi.YapayZeka, null);
    }

    private List<AdayGercekleri> Gercekler(List<AdayOzeti> secilenler)
    {
        var sorular = uyum.Sorular();
        var benim = uyum.Cevaplar(oturum.KullaniciId);
        return secilenler.Select((a, i) =>
        {
            var onun = uyum.Cevaplar(a.KullaniciId);
            var kategoriler = sorular.GroupBy(s => s.Kategori).OrderBy(g => g.Key).Select(g => new KategoriGercegi(
                g.Key,
                Math.Round(UyumHesaplayici.Hesapla(g, benim, onun)),
                Math.Round(g.Average(s => benim[s.Id].Deger), 1),
                Math.Round(g.Average(s => onun[s.Id].Deger), 1),
                Math.Round(g.Average(s => (benim[s.Id].Agirlik + onun[s.Id].Agirlik) / 2.0), 1))).ToList();
            // Ad yerine kod: yapay zekaya kisisel veri (ad, e-posta, hakkinda metni) gonderilmez.
            return new AdayGercekleri($"A{i + 1}", a, kategoriler);
        }).ToList();
    }

    // Yapay zeka olmadan ayni bilgiyi sablonla yazar. Hem yedek hem de karsilastirma icin kullanilir.
    public static OneriSonucu KuralTabanli(List<AdayGercekleri> gercekler, string? aciklama) =>
        new(gercekler.Select(KuralTabanliOneri).ToList(), OneriKaynagi.KuralTabanli, aciklama);

    public static UyumOnerisi KuralTabanliOneri(AdayGercekleri g)
    {
        // Dogrulayicidaki kuralla ayni: uyumu 60'in altindaki konu guclu yon sayilmaz.
        var guclu = g.Kategoriler
            .Where(k => k.Uyum >= OneriDogrulayici.GucluSiniri)
            .OrderByDescending(k => k.Uyum).ThenByDescending(k => k.Onem)
            .Take(2).ToList();
        var zayif = g.Kategoriler.MinBy(k => k.Uyum)!;
        var gerekce = $"Genel uyumunuz %{Yuzde(g.Aday.UyumYuzdesi)}. " + (guclu.Count == 0
            ? "Belirgin biçimde uyumlu olduğunuz bir konu yok; diğer adaylara göre yine de en yakın kişilerden biri."
            : "En uyumlu olduğunuz konular: "
                + string.Join(" ve ", guclu.Select(k => $"{KategoriAdlari.Ad(k.Kategori).ToLowerInvariant()} (%{Yuzde(k.Uyum)})")) + ".");

        SoruKategorisi? dikkat = null;
        string? dikkatNotu = null;
        if (zayif.Uyum < OneriDogrulayici.DikkatSiniri)
        {
            dikkat = zayif.Kategori;
            dikkatNotu = $"{KategoriAdlari.Ad(zayif.Kategori)} konusunda farklısınız (%{Yuzde(zayif.Uyum)}); baştan konuşmanız iyi olur.";
        }
        return Oneri(g, gerekce, guclu.Select(k => k.Kategori).ToList(), dikkat, dikkatNotu);
    }

    private static UyumOnerisi Oneri(AdayGercekleri g, string gerekce, List<SoruKategorisi> guclu, SoruKategorisi? dikkat, string? not) =>
        new(g.Aday.KullaniciId, g.Aday.AdSoyad, g.Aday.Bolum, g.Aday.Sehir, g.Aday.UyumYuzdesi, gerekce, guclu, dikkat, not);

    public static string Yuzde(double d) => Math.Round(d).ToString("0", CultureInfo.InvariantCulture);
}

public static class KategoriAdlari
{
    public static string Ad(SoruKategorisi kategori) => kategori switch
    {
        SoruKategorisi.UykuDuzeni => "Gece düzeni",
        SoruKategorisi.Temizlik => "Düzen ve temizlik",
        SoruKategorisi.Sosyallik => "Sosyallik",
        SoruKategorisi.Gurultu => "Sessizlik isteği",
        SoruKategorisi.Ders => "Evde ders çalışma",
        _ => kategori.ToString(),
    };
}
