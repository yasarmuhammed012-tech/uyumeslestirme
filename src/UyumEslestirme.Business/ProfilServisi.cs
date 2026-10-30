using UyumEslestirme.Data;
using UyumEslestirme.Domain;

namespace UyumEslestirme.Business;

public sealed record ProfilGirdisi(string Bolum, string Sehir, int DogumYili, string? Hakkinda);

public sealed class ProfilServisi(Depo<Profil> profiller, OturumYoneticisi oturum, TimeProvider saat)
{
    public const int AsgariYas = 16;
    public const int AzamiYas = 80;

    public Profil? BenimProfilim()
    {
        oturum.GirisGerekli();
        return profiller.Bul(oturum.KullaniciId);
    }

    // Sahiplik kontrolu: bir kullanici yalnizca kendi profilini, yonetici herkesinkini gorur.
    public Profil? Goruntule(int kullaniciId)
    {
        oturum.GirisGerekli();
        if (kullaniciId != oturum.KullaniciId && oturum.Rol != Rol.Yonetici)
            throw new YetkisizErisimHatasi();
        return profiller.Bul(kullaniciId);
    }

    public void Kaydet(ProfilGirdisi girdi)
    {
        oturum.GirisGerekli();

        var bolum = girdi.Bolum.Trim();
        var sehir = girdi.Sehir.Trim();
        var hakkinda = string.IsNullOrWhiteSpace(girdi.Hakkinda) ? null : girdi.Hakkinda.Trim();
        var yil = saat.GetUtcNow().Year;

        if (bolum.Length is < 2 or > 100)
            throw new IsKuraliHatasi("Bölüm 2-100 karakter olmalıdır.");
        if (sehir.Length is < 2 or > 60)
            throw new IsKuraliHatasi("Şehir 2-60 karakter olmalıdır.");
        if (girdi.DogumYili < yil - AzamiYas || girdi.DogumYili > yil - AsgariYas)
            throw new IsKuraliHatasi($"Doğum yılı {yil - AzamiYas}-{yil - AsgariYas} arasında olmalıdır.");
        if (hakkinda?.Length > 500)
            throw new IsKuraliHatasi("Hakkında alanı en fazla 500 karakter olabilir.");

        var profil = profiller.Bul(oturum.KullaniciId);
        if (profil is null)
        {
            profiller.Ekle(new Profil
            {
                KullaniciId = oturum.KullaniciId,
                Bolum = bolum,
                Sehir = sehir,
                DogumYili = girdi.DogumYili,
                Hakkinda = hakkinda,
            });
            return;
        }

        profil.Bolum = bolum;
        profil.Sehir = sehir;
        profil.DogumYili = girdi.DogumYili;
        profil.Hakkinda = hakkinda;
        profiller.Guncelle(profil);
    }
}
