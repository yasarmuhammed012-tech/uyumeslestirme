using System.Globalization;
using Microsoft.AspNetCore.Components;
using UyumEslestirme.Business;
using UyumEslestirme.Domain;

namespace UyumEslestirme.Web;

public static class Demo
{
    // Web surumu yalnizca sentetik demo verisiyle calisir.
    public const string Parola = "Demo1234";
    public const string Ogrenci = "ogrenci01@ornek.test";
    public const string Yonetici = "yonetici@ornek.test";
}

public static class Arayuz
{
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

    // "Ayse Kaya" -> "AK"; demo adlarinda ("Ornek Ogrenci 03") kisiyi ayirt eden numara gosterilir.
    public static string BasHarfler(string ad)
    {
        var parcalar = ad.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parcalar.Length == 0)
            return "?";
        var son = parcalar[^1];
        if (son.All(char.IsDigit))
            return son;
        return parcalar.Length == 1 ? son[..1] : $"{parcalar[0][0]}{son[0]}";
    }

    // CSS degerleri kulturden bagimsiz yazilir (Turkcede ondalik ayraci virguldur).
    public static string Yuzde(double oran) => (Math.Clamp(oran, 0, 1) * 100).ToString("0.#", CultureInfo.InvariantCulture) + "%";

    public static string Tarih(DateTime utc) => utc.ToLocalTime().ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
}

// Sayfalarin ortak tabani: is katmani hatalarini ekranda gosterilecek mesaja cevirir.
public abstract class SayfaTabani : ComponentBase
{
    [Inject] protected Servisler S { get; set; } = default!;
    [CascadingParameter] protected App Kabuk { get; set; } = default!;

    protected string? Hata { get; set; }
    protected string? Bilgi { get; set; }

    protected bool Dene(Action islem)
    {
        Hata = null;
        Bilgi = null;
        try
        {
            islem();
            return true;
        }
        catch (IsKuraliHatasi h)
        {
            Hata = h.Message;
        }
        catch (YetkisizErisimHatasi h)
        {
            Hata = h.Message;
            if (!S.Oturum.Acik)
                Kabuk.OturumBitti(h.Message);
        }
        return false;
    }
}
