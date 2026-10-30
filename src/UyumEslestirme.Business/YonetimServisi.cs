using UyumEslestirme.Data;
using UyumEslestirme.Domain;

namespace UyumEslestirme.Business;

// Parola ozeti gibi alanlar arayuze hic tasinmaz.
public sealed record KullaniciOzeti(int Id, string AdSoyad, string Eposta, Rol Rol, DateTime OlusturmaTarihi);

public sealed class YonetimServisi(KullaniciDeposu kullanicilar, OturumYoneticisi oturum)
{
    public List<KullaniciOzeti> KullanicilariListele()
    {
        oturum.RolGerekli(Rol.Yonetici);
        return kullanicilar.Listele()
            .OrderBy(k => k.Id)
            .Select(k => new KullaniciOzeti(k.Id, k.AdSoyad, k.Eposta, k.Rol, k.OlusturmaTarihi))
            .ToList();
    }
}
