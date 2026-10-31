using UyumEslestirme.Business;
using UyumEslestirme.Domain;

namespace UyumEslestirme.Desktop;

internal static class Arayuz
{
    // Is katmaninin kullaniciya yonelik hatalarini tek yerde mesaja cevirir.
    public static bool Dene(Action islem)
    {
        try
        {
            islem();
            return true;
        }
        catch (IsKuraliHatasi hata)
        {
            MessageBox.Show(hata.Message, "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (YetkisizErisimHatasi hata)
        {
            MessageBox.Show(hata.Message, "Yetkisiz işlem", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        return false;
    }

    public static string KategoriAdi(SoruKategorisi kategori) => kategori switch
    {
        SoruKategorisi.UykuDuzeni => "Gece düzeni",
        SoruKategorisi.Temizlik => "Düzen ve temizlik",
        SoruKategorisi.Sosyallik => "Sosyallik",
        SoruKategorisi.Gurultu => "Sessizlik isteği",
        SoruKategorisi.Ders => "Evde ders çalışma",
        _ => kategori.ToString(),
    };

    public static string RolAdi(Rol rol) => rol == Rol.Yonetici ? "Yönetici" : "Öğrenci";
}
