using System.Text.RegularExpressions;
using UyumEslestirme.Data;
using UyumEslestirme.Domain;

namespace UyumEslestirme.Business;

public sealed record BildirimOzeti(
    int Id,
    BildirimTuru Tur,
    string Metin,
    bool Okundu,
    DateTime Tarih,
    HariciGonderimDurumu HariciDurum,
    string? HariciHata);

public sealed record BildirimTercihleri(bool EslesmeBildirimi, bool MesajBildirimi, string? TelefonKonusu);

// Telefona gonderilecek bildirim. Metin genel tutulur: ad, e-posta veya mesaj icerigi harici servise gitmez.
public sealed record HariciBildirimIsi(int BildirimId, string Konu, string Baslik, string Metin);

// Harici gonderim istek sirasinda yapilmaz, kuyruga birakilir. Boylece bildirim servisi yavas veya
// kapaliyken istek gonderme, kabul etme ve mesajlasma etkilenmez. Kuyrugu API projesi saglar.
public interface IBildirimKuyrugu
{
    // false: kuyruk dolu, is alinmadi.
    bool Ekle(HariciBildirimIsi is_);
}

public sealed partial class BildirimServisi(
    BildirimDeposu bildirimler,
    OturumYoneticisi oturum,
    TimeProvider saat,
    IBildirimKuyrugu? kuyruk)
{
    public const int ListeAdedi = 50;
    public const string HariciBaslik = "Uyum Eşleştirme";

    // Konu adi tahmin edilemeyecek kadar uzun olmali: ntfy'da konuyu bilen herkes bildirimleri okuyabilir.
    [GeneratedRegex("^[A-Za-z0-9_-]{12,64}$")]
    private static partial Regex KonuKurali();

    public bool HariciKanalVar => kuyruk is not null;

    // Tetikleyiciler (istek, kabul, mesaj) bu metodu cagirir. Uygulama ici bildirim her zaman yazilir;
    // telefona gonderim, kullanicinin tercihi aciksa ve bir konu tanimliysa kuyruga eklenir.
    public void Olustur(int aliciId, BildirimTuru tur, string metin)
    {
        var tercih = bildirimler.Tercih(aliciId);
        var bildirim = new Bildirim
        {
            KullaniciId = aliciId,
            Tur = tur,
            Metin = metin.Length > 300 ? metin[..300] : metin,
            OlusturmaTarihi = saat.GetUtcNow().UtcDateTime,
        };

        var hariciIstendi = kuyruk is not null && !string.IsNullOrEmpty(tercih?.TelefonKonusu) && tur switch
        {
            BildirimTuru.EslesmeIstegi or BildirimTuru.IstekKabul => tercih.EslesmeBildirimi,
            BildirimTuru.YeniMesaj => tercih.MesajBildirimi,
            _ => true,
        };
        bildirim.HariciDurum = hariciIstendi ? HariciGonderimDurumu.Bekliyor : HariciGonderimDurumu.Kapali;
        bildirimler.Ekle(bildirim);

        if (hariciIstendi && !kuyruk!.Ekle(new HariciBildirimIsi(bildirim.Id, tercih!.TelefonKonusu!, HariciBaslik, HariciMetin(tur))))
        {
            bildirim.HariciDurum = HariciGonderimDurumu.Basarisiz;
            bildirim.HariciHata = "Bildirim kuyruğu dolu, telefona gönderilemedi.";
            bildirimler.Guncelle(bildirim);
        }
    }

    public static string HariciMetin(BildirimTuru tur) => tur switch
    {
        BildirimTuru.EslesmeIstegi => "Yeni bir eşleşme isteğin var.",
        BildirimTuru.IstekKabul => "Bir eşleşme isteğin kabul edildi.",
        BildirimTuru.YeniMesaj => "Yeni bir mesajın var.",
        _ => "Deneme bildirimi: telefon bildirimleri çalışıyor.",
    };

    public List<BildirimOzeti> Listele()
    {
        oturum.GirisGerekli();
        return bildirimler.KullanicininBildirimleri(oturum.KullaniciId, ListeAdedi)
            .Select(b => new BildirimOzeti(b.Id, b.Tur, b.Metin, b.Okundu, b.OlusturmaTarihi, b.HariciDurum, b.HariciHata))
            .ToList();
    }

    public int OkunmamisSayisi()
    {
        oturum.GirisGerekli();
        return bildirimler.OkunmamisSayisi(oturum.KullaniciId);
    }

    public void TumunuOkundu()
    {
        oturum.GirisGerekli();
        bildirimler.TumunuOkundu(oturum.KullaniciId);
    }

    public BildirimTercihleri Tercihlerim()
    {
        oturum.GirisGerekli();
        var t = bildirimler.Tercih(oturum.KullaniciId) ?? new BildirimTercihi();
        return new BildirimTercihleri(t.EslesmeBildirimi, t.MesajBildirimi, t.TelefonKonusu);
    }

    public void TercihleriKaydet(BildirimTercihleri girdi)
    {
        oturum.GirisGerekli();
        var konu = string.IsNullOrWhiteSpace(girdi.TelefonKonusu) ? null : girdi.TelefonKonusu.Trim();
        if (konu is not null && !KonuKurali().IsMatch(konu))
            throw new IsKuraliHatasi("Telefon konusu 12-64 karakter olmalı; yalnızca harf, rakam, - ve _ içerebilir.");

        var tercih = bildirimler.Tercih(oturum.KullaniciId) ?? new BildirimTercihi { KullaniciId = oturum.KullaniciId };
        tercih.EslesmeBildirimi = girdi.EslesmeBildirimi;
        tercih.MesajBildirimi = girdi.MesajBildirimi;
        tercih.TelefonKonusu = konu;
        bildirimler.TercihKaydet(tercih);
    }

    // Kullanici ayarini dogrulamak icin kendi telefonuna deneme bildirimi gonderir.
    public void DenemeGonder()
    {
        oturum.GirisGerekli();
        if (kuyruk is null)
            throw new IsKuraliHatasi("Telefon bildirimi yalnızca sunucu sürümünde gönderilir.");
        if (string.IsNullOrEmpty(bildirimler.Tercih(oturum.KullaniciId)?.TelefonKonusu))
            throw new IsKuraliHatasi("Önce bir telefon konusu girip kaydedin.");
        Olustur(oturum.KullaniciId, BildirimTuru.Deneme, "Deneme bildirimi gönderildi.");
    }
}
