using Microsoft.EntityFrameworkCore;
using UyumEslestirme.Business;

namespace UyumEslestirme.Api;

// Is katmaninin hatalarini HTTP durum kodlarina cevirir. Mesajlar masaustundekiyle aynidir:
// ornegin olmayan, gizli veya uygun olmayan kisiye istek gonderilince yine tek mesaj doner.
public sealed class HataKarsilayici(RequestDelegate sonraki, ILogger<HataKarsilayici> gunluk)
{
    public async Task InvokeAsync(HttpContext http)
    {
        try
        {
            await sonraki(http);
        }
        catch (IsKuraliHatasi h)
        {
            await Yaz(http, StatusCodes.Status400BadRequest, h.Message);
        }
        catch (YetkisizErisimHatasi h)
        {
            // Token yoksa 401 (giris gerekli), token var ama yetki yoksa 403.
            var girisli = http.User.Identity?.IsAuthenticated == true;
            await Yaz(http, girisli ? StatusCodes.Status403Forbidden : StatusCodes.Status401Unauthorized, h.Message);
        }
        catch (DbUpdateException h)
        {
            // Ornek: iki kisi ayni anda birbirine istek gonderdi; benzersiz indeks ikinci kaydi reddeder.
            gunluk.LogWarning(h, "Veritabanı çakışması");
            await Yaz(http, StatusCodes.Status409Conflict, "İşlem başka bir işlemle çakıştı. Sayfayı yenileyip tekrar deneyin.");
        }
    }

    private static Task Yaz(HttpContext http, int kod, string mesaj)
    {
        http.Response.StatusCode = kod;
        return http.Response.WriteAsJsonAsync(new HataYaniti(mesaj));
    }
}
