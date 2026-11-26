using UyumEslestirme.Data;
using UyumEslestirme.Domain;

namespace UyumEslestirme.Business;

public sealed record MesajOzeti(int Id, bool Benim, string Icerik, DateTime Tarih, bool Okundu);

public sealed record MesajGonderIstegi(int KisiId, string Icerik);

// Mesajlasma yalnizca karsilikli eslesmis (Kabul) iki ogrenci arasinda yapilabilir.
public sealed class MesajServisi(
    MesajDeposu mesajlar,
    EslesmeDeposu eslesmeler,
    BildirimServisi bildirim,
    OturumYoneticisi oturum,
    TimeProvider saat)
{
    public const int AzamiUzunluk = 1000;
    public const int DakikadaAzamiMesaj = 20;
    public const int KonusmaAdedi = 100;

    public void Gonder(int kisiId, string icerik)
    {
        oturum.RolGerekli(Rol.Ogrenci);
        icerik = icerik.Trim();
        if (icerik.Length == 0)
            throw new IsKuraliHatasi("Boş mesaj gönderilemez.");
        if (icerik.Length > AzamiUzunluk)
            throw new IsKuraliHatasi($"Mesaj en fazla {AzamiUzunluk} karakter olabilir.");
        EslesmisOlmali(kisiId);

        var simdi = saat.GetUtcNow().UtcDateTime;
        if (mesajlar.GonderilenSayisi(oturum.KullaniciId, simdi.AddMinutes(-1)) >= DakikadaAzamiMesaj)
            throw new IsKuraliHatasi("Çok hızlı mesaj gönderiyorsunuz. Lütfen biraz bekleyin.");

        mesajlar.Ekle(new Mesaj
        {
            GonderenId = oturum.KullaniciId,
            AliciId = kisiId,
            Icerik = icerik,
            GonderimTarihi = simdi,
        });
        bildirim.Olustur(kisiId, BildirimTuru.YeniMesaj, $"{oturum.AdSoyad} size mesaj gönderdi.");
    }

    // Konusmayi dondurur ve karsi tarafin mesajlarini okundu yapar.
    public List<MesajOzeti> Konusma(int kisiId)
    {
        oturum.RolGerekli(Rol.Ogrenci);
        EslesmisOlmali(kisiId);
        var ben = oturum.KullaniciId;
        var liste = mesajlar.Arasinda(ben, kisiId, KonusmaAdedi)
            .Select(m => new MesajOzeti(m.Id, m.GonderenId == ben, m.Icerik, m.GonderimTarihi, m.Okundu))
            .ToList();
        mesajlar.OkunduIsaretle(ben, kisiId);
        return liste;
    }

    // Olmayan, eslesmemis veya reddedilmis kisi icin ayni mesaj: kimin var oldugu sizmaz.
    private void EslesmisOlmali(int kisiId)
    {
        if (eslesmeler.Arasinda(oturum.KullaniciId, kisiId) is not { Durum: EslesmeDurumu.Kabul })
            throw new IsKuraliHatasi("Yalnızca eşleştiğiniz kişilerle mesajlaşabilirsiniz.");
    }
}
