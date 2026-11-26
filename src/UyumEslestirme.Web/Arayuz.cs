using System.Globalization;
using Microsoft.AspNetCore.Components;
using UyumEslestirme.Business;
using UyumEslestirme.Domain;
using UyumEslestirme.Web.Istemci;

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
    public static string KategoriAdi(SoruKategorisi kategori) => KategoriAdlari.Ad(kategori);

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

// Sayfalarin ortak tabani: is katmani (veya API) hatalarini ekranda gosterilecek mesaja cevirir.
public abstract class SayfaTabani : ComponentBase
{
    [Inject] protected IUygulama U { get; set; } = default!;
    [CascadingParameter] protected App Kabuk { get; set; } = default!;

    protected string? Hata { get; set; }
    protected string? Bilgi { get; set; }
    protected bool Mesgul { get; private set; }

    protected async Task<bool> Dene(Func<Task> islem)
    {
        Hata = null;
        Bilgi = null;
        Mesgul = true;
        try
        {
            await islem();
            return true;
        }
        catch (IsKuraliHatasi h)
        {
            Hata = h.Message;
        }
        catch (YetkisizErisimHatasi h)
        {
            Hata = h.Message;
            if (!U.Acik)
                await Kabuk.OturumBitti(h.Message);
        }
        finally
        {
            Mesgul = false;
        }
        return false;
    }
}
