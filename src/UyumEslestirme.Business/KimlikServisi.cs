using System.Net.Mail;
using UyumEslestirme.Data;
using UyumEslestirme.Domain;

namespace UyumEslestirme.Business;

public sealed class KimlikServisi(KullaniciDeposu kullanicilar, OturumYoneticisi oturum, TimeProvider saat)
{
    public const int AzamiDeneme = 5;
    public const int AsgariParolaUzunlugu = 8;
    public const int AzamiParolaUzunlugu = 128;
    public static readonly TimeSpan KilitSuresi = TimeSpan.FromMinutes(15);

    private const string HataliGiris = "E-posta veya parola hatalı.";

    // Olmayan e-postada da ayni sure harcansin diye karsilastirilan sahte kayit.
    private static readonly Lazy<string> SahteHash = new(() => ParolaHasleyici.Hashle(Guid.NewGuid().ToString()));

    public void KayitOl(string adSoyad, string eposta, string parola, string parolaTekrar)
    {
        adSoyad = adSoyad.Trim();
        eposta = EpostaDuzenle(eposta);

        if (adSoyad.Length is < 2 or > 100)
            throw new IsKuraliHatasi("Ad soyad 2-100 karakter olmalıdır.");
        if (eposta.Length > 200 || !MailAddress.TryCreate(eposta, out var adres) || adres.Address != eposta)
            throw new IsKuraliHatasi("Geçerli bir e-posta adresi girin.");
        if (parola != parolaTekrar)
            throw new IsKuraliHatasi("Parolalar eşleşmiyor.");
        if (parola.Length is < AsgariParolaUzunlugu or > AzamiParolaUzunlugu
            || !parola.Any(char.IsLetter) || !parola.Any(char.IsDigit))
        {
            throw new IsKuraliHatasi($"Parola {AsgariParolaUzunlugu}-{AzamiParolaUzunlugu} karakter olmalı, harf ve rakam içermelidir.");
        }
        if (kullanicilar.EpostaVarMi(eposta))
            throw new IsKuraliHatasi("Bu e-posta adresi zaten kayıtlı.");

        // Rol disaridan alinmaz: kayit olan herkes ogrencidir, yonetici yetkisi kayit ekranindan verilemez.
        kullanicilar.Ekle(new Kullanici
        {
            AdSoyad = adSoyad,
            Eposta = eposta,
            ParolaHash = ParolaHasleyici.Hashle(parola),
            Rol = Rol.Ogrenci,
            OlusturmaTarihi = saat.GetUtcNow().UtcDateTime,
        });
    }

    public void GirisYap(string eposta, string parola)
    {
        var simdi = saat.GetUtcNow().UtcDateTime;
        var kullanici = kullanicilar.EpostaIleBul(EpostaDuzenle(eposta));

        if (kullanici is null)
        {
            ParolaHasleyici.Dogrula(parola, SahteHash.Value);
            throw new IsKuraliHatasi(HataliGiris);
        }

        if (kullanici.KilitBitis > simdi)
            throw new IsKuraliHatasi("Çok fazla başarısız deneme yapıldı. Lütfen daha sonra tekrar deneyin.");

        if (!ParolaHasleyici.Dogrula(parola, kullanici.ParolaHash))
        {
            kullanici.BasarisizGiris++;
            if (kullanici.BasarisizGiris >= AzamiDeneme)
            {
                kullanici.KilitBitis = simdi + KilitSuresi;
                kullanici.BasarisizGiris = 0;
            }
            kullanicilar.Guncelle(kullanici);
            throw new IsKuraliHatasi(HataliGiris);
        }

        kullanici.BasarisizGiris = 0;
        kullanici.KilitBitis = null;
        kullanicilar.Guncelle(kullanici);
        oturum.Ac(kullanici.Id, kullanici.AdSoyad, kullanici.Rol);
    }

    public void CikisYap() => oturum.Kapat();

    private static string EpostaDuzenle(string eposta) => eposta.Trim().ToLowerInvariant();
}
