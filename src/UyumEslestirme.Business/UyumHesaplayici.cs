using UyumEslestirme.Domain;

namespace UyumEslestirme.Business;

// Skorlama fonksiyonu: veritabanina ve oturuma bagli degildir, yalnizca cevaplardan hesaplar.
public static class UyumHesaplayici
{
    // Her soru icin benzerlik: ayni cevap 1, en uzak cevap (1 ile 5) 0.
    // Sorunun agirligi iki kisinin verdigi onem degerinin ortalamasidir; boylece skor iki yonde de aynidir.
    // Sonuc: 0-100 arasi agirlikli ortalama.
    public static double Hesapla(
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
        return toplamAgirlik == 0 ? 0 : toplam / toplamAgirlik * 100;
    }

    public static bool TamamMi(IEnumerable<AnketSorusu> sorular, IReadOnlyDictionary<int, AnketCevabi> cevaplar) =>
        sorular.All(s => cevaplar.ContainsKey(s.Id));
}
