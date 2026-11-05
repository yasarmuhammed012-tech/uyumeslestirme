using UyumEslestirme.Data;
using UyumEslestirme.Domain;

namespace UyumEslestirme.Business;

public enum AdayDurumu
{
    Yok = 0,
    IstekGonderildi = 1,
    IstekGeldi = 2,
    Eslesti = 3,
}

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
    SoruKategorisi EnFarkliKategori,
    AdayDurumu Durum);

public sealed class AdayServisi(
    KullaniciDeposu kullanicilar,
    EslesmeDeposu eslesmeler,
    UyumServisi uyum,
    OturumYoneticisi oturum,
    TimeProvider saat)
{
    // Profilini ve anketini tamamlamis, profilini gizlememis diger ogrencileri uyuma gore siralar.
    public List<AdayOzeti> Adaylar()
    {
        oturum.GirisGerekli();

        var sorular = uyum.Sorular();
        if (sorular.Count == 0)
            return [];

        var ben = oturum.KullaniciId;
        var benim = uyum.Cevaplar(ben);
        if (!UyumHesaplayici.TamamMi(sorular, benim))
            throw new IsKuraliHatasi("Uyumlu kişileri görmek için önce tercih anketini tamamlayın.");

        var iliskiler = new Dictionary<int, Eslesme>();
        foreach (var e in eslesmeler.KullanicininEslesmeleri(ben))
            iliskiler.TryAdd(e.GonderenId == ben ? e.AliciId : e.GonderenId, e);

        var yil = saat.GetUtcNow().Year;
        var adaylar = new List<AdayOzeti>();
        foreach (var kullanici in kullanicilar.ProfilliOgrenciler(ben))
        {
            if (!kullanici.Profil!.Gorunur)
                continue;

            var cevaplar = kullanici.Cevaplar.ToDictionary(c => c.SoruId);
            if (!UyumHesaplayici.TamamMi(sorular, cevaplar))
                continue;

            iliskiler.TryGetValue(kullanici.Id, out var iliski);
            var durum = Durum(iliski, ben);
            if (durum is null)
                continue;

            var kategoriler = sorular
                .GroupBy(s => s.Kategori)
                .Select(g => (Kategori: g.Key, Uyum: UyumHesaplayici.Hesapla(g, benim, cevaplar)))
                .ToList();

            adaylar.Add(new AdayOzeti(
                kullanici.Id,
                kullanici.AdSoyad,
                kullanici.Profil.Bolum,
                kullanici.Profil.Sehir,
                yil - kullanici.Profil.DogumYili,
                kullanici.Profil.Hakkinda,
                uyum.Skor(sorular, ben, benim, kullanici.Id, cevaplar)!.Value,
                kategoriler.MaxBy(k => k.Uyum).Kategori,
                kategoriler.MinBy(k => k.Uyum).Kategori,
                durum.Value));
        }

        return adaylar
            .OrderByDescending(a => a.UyumYuzdesi)
            .ThenBy(a => a.AdSoyad)
            .ToList();
    }

    // Reddedilmis bir istek varsa iki taraf da birbirini listede gormez (null).
    private static AdayDurumu? Durum(Eslesme? iliski, int ben) => iliski?.Durum switch
    {
        null => AdayDurumu.Yok,
        EslesmeDurumu.Kabul => AdayDurumu.Eslesti,
        EslesmeDurumu.Red => null,
        _ => iliski.GonderenId == ben ? AdayDurumu.IstekGonderildi : AdayDurumu.IstekGeldi,
    };
}
