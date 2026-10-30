using UyumEslestirme.Data;
using UyumEslestirme.Domain;

namespace UyumEslestirme.Business;

public sealed record CevapGirdisi(int SoruId, int Deger, int Agirlik);

public sealed record KategoriOzeti(SoruKategorisi Kategori, double Ortalama, double AgirlikYuzdesi);

// Metotlar kullanici kimligini parametre olarak almaz, oturumdan okur.
// Boylece bir kullanici baska birinin cevaplarini okuyamaz ve degistiremez.
public sealed class AnketServisi(AnketDeposu anket, OturumYoneticisi oturum)
{
    public const int AsgariOlcek = 1;
    public const int AzamiOlcek = 5;

    public List<AnketSorusu> Sorular()
    {
        oturum.GirisGerekli();
        return anket.AktifSorular();
    }

    public List<AnketCevabi> BenimCevaplarim()
    {
        oturum.GirisGerekli();
        return anket.KullaniciCevaplari(oturum.KullaniciId);
    }

    public void CevaplariKaydet(IReadOnlyList<CevapGirdisi> girdiler)
    {
        oturum.GirisGerekli();

        foreach (var g in girdiler)
        {
            if (g.Deger is < AsgariOlcek or > AzamiOlcek || g.Agirlik is < AsgariOlcek or > AzamiOlcek)
                throw new IsKuraliHatasi($"Cevap ve önem değerleri {AsgariOlcek}-{AzamiOlcek} arasında olmalıdır.");
        }

        var aktifSorular = anket.AktifSorular().Select(s => s.Id).ToHashSet();
        var cevaplanan = girdiler.Select(g => g.SoruId).ToList();

        if (cevaplanan.Count != cevaplanan.Distinct().Count())
            throw new IsKuraliHatasi("Aynı soru birden fazla kez cevaplanamaz.");
        if (cevaplanan.Any(id => !aktifSorular.Contains(id)))
            throw new IsKuraliHatasi("Geçersiz veya kullanımdan kalkmış bir soru cevaplandı.");
        if (cevaplanan.Count != aktifSorular.Count)
            throw new IsKuraliHatasi("Tüm sorular cevaplanmalıdır.");

        var kullaniciId = oturum.KullaniciId;
        anket.CevaplariDegistir(kullaniciId, girdiler
            .Select(g => new AnketCevabi { KullaniciId = kullaniciId, SoruId = g.SoruId, Deger = g.Deger, Agirlik = g.Agirlik })
            .ToList());
    }

    public bool TamamlandiMi()
    {
        var aktifSorular = Sorular().Select(s => s.Id).ToHashSet();
        return aktifSorular.All(BenimCevaplarim().Select(c => c.SoruId).ToHashSet().Contains);
    }

    // Ortalama: onem derecesiyle agirliklandirilmis cevap ortalamasi (1-5).
    // AgirlikYuzdesi: kategorinin, kullanicinin verdigi toplam onem icindeki payi.
    public List<KategoriOzeti> TercihOzeti()
    {
        var cevaplar = BenimCevaplarim();
        double toplamAgirlik = cevaplar.Sum(c => c.Agirlik);

        return cevaplar
            .GroupBy(c => c.Soru!.Kategori)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                double agirlik = g.Sum(c => c.Agirlik);
                return new KategoriOzeti(
                    g.Key,
                    g.Sum(c => (double)c.Deger * c.Agirlik) / agirlik,
                    agirlik / toplamAgirlik * 100);
            })
            .ToList();
    }
}
