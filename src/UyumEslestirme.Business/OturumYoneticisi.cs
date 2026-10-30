using UyumEslestirme.Domain;

namespace UyumEslestirme.Business;

// Masaustu uygulamasi tek surecte calistigi icin oturum bellekte tutulur; JWT gibi bir token gerekmez.
public sealed class OturumYoneticisi(TimeProvider saat)
{
    public static readonly TimeSpan OturumSuresi = TimeSpan.FromMinutes(30);

    private DateTimeOffset _bitis;

    public int KullaniciId { get; private set; }
    public string AdSoyad { get; private set; } = "";
    public Rol Rol { get; private set; }

    public bool Acik => KullaniciId != 0 && saat.GetUtcNow() < _bitis;

    public void Ac(int kullaniciId, string adSoyad, Rol rol)
    {
        KullaniciId = kullaniciId;
        AdSoyad = adSoyad;
        Rol = rol;
        _bitis = saat.GetUtcNow() + OturumSuresi;
    }

    public void Kapat()
    {
        KullaniciId = 0;
        AdSoyad = "";
        Rol = Rol.Ogrenci;
    }

    public void GirisGerekli()
    {
        if (!Acik)
            throw new YetkisizErisimHatasi("Oturum açık değil veya süresi doldu. Lütfen tekrar giriş yapın.");
    }

    public void RolGerekli(Rol rol)
    {
        GirisGerekli();
        if (Rol != rol)
            throw new YetkisizErisimHatasi();
    }
}
