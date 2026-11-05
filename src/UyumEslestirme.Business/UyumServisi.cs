using UyumEslestirme.Data;
using UyumEslestirme.Domain;

namespace UyumEslestirme.Business;

// Skorlari UyumSkoru tablosunda onbellekte tutar. Bir kullanicinin cevaplari degisince
// AnketDeposu o kullaniciya ait skorlari siler; skor ilk istendiginde yeniden hesaplanir.
public sealed class UyumServisi(AnketDeposu anket, UyumSkoruDeposu skorlar, TimeProvider saat)
{
    public List<AnketSorusu> Sorular() => anket.AktifSorular();

    public Dictionary<int, AnketCevabi> Cevaplar(int kullaniciId) =>
        anket.KullaniciCevaplari(kullaniciId).ToDictionary(c => c.SoruId);

    public bool AnketTamamMi(int kullaniciId)
    {
        var sorular = Sorular();
        return sorular.Count > 0 && UyumHesaplayici.TamamMi(sorular, Cevaplar(kullaniciId));
    }

    // Iki taraftan biri anketi tamamlamamissa null doner.
    public double? Skor(int a, int b) => Skor(Sorular(), a, Cevaplar(a), b, Cevaplar(b));

    public double? Skor(
        List<AnketSorusu> sorular,
        int a, IReadOnlyDictionary<int, AnketCevabi> ca,
        int b, IReadOnlyDictionary<int, AnketCevabi> cb)
    {
        if (sorular.Count == 0 || !UyumHesaplayici.TamamMi(sorular, ca) || !UyumHesaplayici.TamamMi(sorular, cb))
            return null;

        var kayit = skorlar.Bul(a, b);
        if (kayit is not null)
            return kayit.Skor;

        var skor = UyumHesaplayici.Hesapla(sorular, ca, cb);
        skorlar.Kaydet(a, b, skor, saat.GetUtcNow().UtcDateTime);
        return skor;
    }
}
