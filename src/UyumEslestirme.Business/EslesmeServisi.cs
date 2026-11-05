using UyumEslestirme.Data;
using UyumEslestirme.Domain;

namespace UyumEslestirme.Business;

// Eposta yalnizca karsilikli eslesmeden (Kabul) sonra doldurulur.
public sealed record EslesmeOzeti(
    int EslesmeId,
    int KisiId,
    string AdSoyad,
    string Bolum,
    string Sehir,
    double? UyumYuzdesi,
    EslesmeDurumu Durum,
    string? Eposta,
    DateTime Tarih);

public sealed record EslesmeListeleri(
    List<EslesmeOzeti> GelenIstekler,
    List<EslesmeOzeti> GonderilenIstekler,
    List<EslesmeOzeti> Eslesmeler);

public sealed class EslesmeServisi(
    EslesmeDeposu eslesmeler,
    KullaniciDeposu kullanicilar,
    UyumServisi uyum,
    OturumYoneticisi oturum,
    TimeProvider saat)
{
    public const int AzamiBekleyenIstek = 10;

    public void IstekGonder(int aliciId)
    {
        oturum.RolGerekli(Rol.Ogrenci);
        var ben = oturum.KullaniciId;

        if (aliciId == ben)
            throw new IsKuraliHatasi("Kendinize istek gönderemezsiniz.");
        if (!uyum.AnketTamamMi(ben))
            throw new IsKuraliHatasi("İstek göndermek için önce tercih anketini tamamlayın.");

        // Olmayan, gizli veya uygun olmayan kullanici icin ayni mesaj: kimin var oldugu sizmaz.
        var alici = kullanicilar.ProfilIleBul(aliciId);
        if (alici is null || alici.Rol != Rol.Ogrenci || alici.Profil is not { Gorunur: true } || !uyum.AnketTamamMi(aliciId))
            throw new IsKuraliHatasi("Bu kişiye istek gönderilemez.");

        var mevcut = eslesmeler.Arasinda(ben, aliciId);
        if (mevcut is not null)
        {
            throw new IsKuraliHatasi(mevcut.Durum switch
            {
                EslesmeDurumu.Kabul => "Bu kişiyle zaten eşleştiniz.",
                EslesmeDurumu.Beklemede when mevcut.GonderenId == ben => "Bu kişiye zaten istek gönderdiniz.",
                EslesmeDurumu.Beklemede => "Bu kişi size zaten istek gönderdi. Eşleşmelerim sayfasından yanıtlayabilirsiniz.",
                _ => "Bu kişiye istek gönderilemez.",
            });
        }

        if (eslesmeler.BekleyenGidenSayisi(ben) >= AzamiBekleyenIstek)
            throw new IsKuraliHatasi($"Yanıt bekleyen en fazla {AzamiBekleyenIstek} isteğiniz olabilir. Önce bazılarını geri çekin.");

        eslesmeler.Ekle(new Eslesme
        {
            GonderenId = ben,
            AliciId = aliciId,
            Durum = EslesmeDurumu.Beklemede,
            OlusturmaTarihi = saat.GetUtcNow().UtcDateTime,
        });
    }

    // Yalnizca istegin alicisi yanitlayabilir.
    public void Yanitla(int eslesmeId, bool kabul)
    {
        oturum.RolGerekli(Rol.Ogrenci);
        var eslesme = eslesmeler.Bul(eslesmeId);
        if (eslesme is null || eslesme.AliciId != oturum.KullaniciId)
            throw new YetkisizErisimHatasi();
        if (eslesme.Durum != EslesmeDurumu.Beklemede)
            throw new IsKuraliHatasi("Bu istek zaten yanıtlandı.");

        eslesme.Durum = kabul ? EslesmeDurumu.Kabul : EslesmeDurumu.Red;
        eslesmeler.Guncelle(eslesme);
    }

    // Yalnizca gonderen, henuz yanitlanmamis istegi geri cekebilir.
    public void GeriCek(int eslesmeId)
    {
        oturum.RolGerekli(Rol.Ogrenci);
        var eslesme = eslesmeler.Bul(eslesmeId);
        if (eslesme is null || eslesme.GonderenId != oturum.KullaniciId)
            throw new YetkisizErisimHatasi();
        if (eslesme.Durum != EslesmeDurumu.Beklemede)
            throw new IsKuraliHatasi("Yalnızca yanıt bekleyen istekler geri çekilebilir.");

        eslesmeler.Sil(eslesme);
    }

    public EslesmeListeleri Listele()
    {
        oturum.GirisGerekli();
        var ben = oturum.KullaniciId;
        var tumu = eslesmeler.KullanicininEslesmeleri(ben);

        EslesmeOzeti Ozet(Eslesme e)
        {
            var diger = e.GonderenId == ben ? e.Alici! : e.Gonderen!;
            return new EslesmeOzeti(
                e.Id,
                diger.Id,
                diger.AdSoyad,
                diger.Profil?.Bolum ?? "",
                diger.Profil?.Sehir ?? "",
                uyum.Skor(ben, diger.Id),
                e.Durum,
                e.Durum == EslesmeDurumu.Kabul ? diger.Eposta : null,
                e.OlusturmaTarihi);
        }

        return new EslesmeListeleri(
            tumu.Where(e => e.AliciId == ben && e.Durum == EslesmeDurumu.Beklemede).Select(Ozet).ToList(),
            tumu.Where(e => e.GonderenId == ben && e.Durum != EslesmeDurumu.Kabul).Select(Ozet).ToList(),
            tumu.Where(e => e.Durum == EslesmeDurumu.Kabul).Select(Ozet).ToList());
    }
}
