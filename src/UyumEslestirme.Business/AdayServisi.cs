using UyumEslestirme.Data;
using UyumEslestirme.Domain;

namespace UyumEslestirme.Business;

// E-posta, dogum yili ve tek tek anket cevaplari bilincli olarak disariya verilmez.
public sealed record AdayOzeti(
    int KullaniciId,
    string AdSoyad,
    string Bolum,
    string Sehir,
    int Yas,
    string? Hakkinda,
    double UyumYuzdesi,
    SoruKategorisi EnUyumluKategori,
    SoruKategorisi EnFarkliKategori);

public sealed class AdayServisi(KullaniciDeposu kullanicilar, AnketDeposu anket, OturumYoneticisi oturum, TimeProvider saat)
{
    // Profilini ve anketini tamamlamis diger ogrencileri uyuma gore siralar.
    public List<AdayOzeti> Adaylar()
    {
        oturum.GirisGerekli();

        var sorular = anket.AktifSorular();
        if (sorular.Count == 0)
            return [];

        var benim = anket.KullaniciCevaplari(oturum.KullaniciId).ToDictionary(c => c.SoruId);
        if (!TamamMi(sorular, benim))
            throw new IsKuraliHatasi("Uyumlu kişileri görmek için önce tercih anketini tamamlayın.");

        var yil = saat.GetUtcNow().Year;
        var adaylar = new List<AdayOzeti>();
        foreach (var kullanici in kullanicilar.ProfilliOgrenciler(oturum.KullaniciId))
        {
            var cevaplar = kullanici.Cevaplar.ToDictionary(c => c.SoruId);
            if (!TamamMi(sorular, cevaplar))
                continue;

            var kategoriler = sorular
                .GroupBy(s => s.Kategori)
                .Select(g => (Kategori: g.Key, Uyum: Uyum(g, benim, cevaplar)))
                .ToList();

            adaylar.Add(new AdayOzeti(
                kullanici.Id,
                kullanici.AdSoyad,
                kullanici.Profil!.Bolum,
                kullanici.Profil.Sehir,
                yil - kullanici.Profil.DogumYili,
                kullanici.Profil.Hakkinda,
                Uyum(sorular, benim, cevaplar),
                kategoriler.MaxBy(k => k.Uyum).Kategori,
                kategoriler.MinBy(k => k.Uyum).Kategori));
        }

        return adaylar
            .OrderByDescending(a => a.UyumYuzdesi)
            .ThenBy(a => a.AdSoyad)
            .ToList();
    }

    // Her soru icin benzerlik: ayni cevap 1, en uzak cevap (1 ile 5) 0.
    // Sorunun agirligi iki kisinin verdigi onem degerinin ortalamasidir; boylece skor iki yonde de aynidir.
    // Sonuc: 0-100 arasi agirlikli ortalama.
    internal static double Uyum(
        IEnumerable<AnketSorusu> sorular,
        IReadOnlyDictionary<int, AnketCevabi> a,
        IReadOnlyDictionary<int, AnketCevabi> b)
    {
        double toplam = 0, toplamAgirlik = 0;
        foreach (var soru in sorular)
        {
            var (ca, cb) = (a[soru.Id], b[soru.Id]);
            var benzerlik = 1 - Math.Abs(ca.Deger - cb.Deger) / (double)(AnketServisi.AzamiOlcek - AnketServisi.AsgariOlcek);
            var agirlik = (ca.Agirlik + cb.Agirlik) / 2.0;
            toplam += benzerlik * agirlik;
            toplamAgirlik += agirlik;
        }
        return toplam / toplamAgirlik * 100;
    }

    private static bool TamamMi(List<AnketSorusu> sorular, Dictionary<int, AnketCevabi> cevaplar) =>
        sorular.All(s => cevaplar.ContainsKey(s.Id));
}
