using System.Net;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using UyumEslestirme.Business;

namespace UyumEslestirme.Api.Bildirim;

public sealed class BildirimAyarlari
{
    public const string Bolum = "Bildirim";

    // ntfy sunucusu (REST). Kendi sunucunuz varsa adresi degistirilebilir.
    public string Adres { get; set; } = "https://ntfy.sh/";

    // Erisim anahtari (ntfy access token). Koda ve depoya yazilmaz: Bildirim__Anahtar ortam degiskeni
    // veya gelistirmede user-secrets. Bos ise anahtarsiz (herkese acik konu) gonderilir.
    public string Anahtar { get; set; } = "";

    public int ZamanAsimiSaniye { get; set; } = 5;
    public int AzamiDeneme { get; set; } = 3;

    // Yeniden denemeler arasindaki bekleme: 1 sn, 2 sn ... (testlerde 0).
    public int BeklemeMilisaniye { get; set; } = 1000;
}

// Kalici hata (ornek: anahtar gecersiz) yeniden denenmez; gecici hata (zaman asimi, 5xx, 429) denenir.
public sealed class BildirimGonderimHatasi(string mesaj, bool kalici) : Exception(mesaj)
{
    public bool Kalici { get; } = kalici;
}

public interface IHariciBildirimKanali
{
    Task Gonder(HariciBildirimIsi is_, CancellationToken iptal);
}

// ntfy.sh REST API'sine JSON ile bildirim yayinlar: POST / {"topic","title","message",...}
// Telefonda ntfy uygulamasi kurulup ayni konuya abone olununca bildirim anlik gelir.
public sealed class NtfyKanali(HttpClient http, BildirimAyarlari ayarlar, ILogger<NtfyKanali> gunluk) : IHariciBildirimKanali
{
    private static readonly TimeSpan AzamiRetryAfter = TimeSpan.FromSeconds(10);

    public async Task Gonder(HariciBildirimIsi is_, CancellationToken iptal)
    {
        string sonHata = "";
        for (var deneme = 1; deneme <= ayarlar.AzamiDeneme; deneme++)
        {
            TimeSpan? bekle = null;
            try
            {
                using var sure = CancellationTokenSource.CreateLinkedTokenSource(iptal);
                sure.CancelAfter(TimeSpan.FromSeconds(ayarlar.ZamanAsimiSaniye));

                using var istek = new HttpRequestMessage(HttpMethod.Post, "")
                {
                    Content = JsonContent.Create(new NtfyIstegi(is_.Konu, is_.Baslik, is_.Metin, ["house"], 3)),
                };
                if (!string.IsNullOrEmpty(ayarlar.Anahtar))
                    istek.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ayarlar.Anahtar);

                using var yanit = await http.SendAsync(istek, sure.Token);
                if (yanit.IsSuccessStatusCode)
                {
                    await YanitiDenetle(yanit, is_, sure.Token);
                    return;
                }

                var kod = (int)yanit.StatusCode;
                sonHata = kod switch
                {
                    401 or 403 => "Bildirim servisi anahtarı reddetti (401/403).",
                    429 => "Bildirim servisinin hız sınırı aşıldı (429).",
                    >= 500 => $"Bildirim servisi geçici hata verdi ({kod}).",
                    _ => $"Bildirim servisi isteği reddetti ({kod}).",
                };

                // 4xx (429 haric) istegin kendisindeki sorundur, tekrar denemek ise yaramaz.
                if (kod is >= 400 and < 500 and not 429)
                    throw new BildirimGonderimHatasi(sonHata, kalici: true);

                if (yanit.StatusCode == HttpStatusCode.TooManyRequests && yanit.Headers.RetryAfter?.Delta is { } d)
                    bekle = d < AzamiRetryAfter ? d : AzamiRetryAfter;
            }
            catch (OperationCanceledException) when (!iptal.IsCancellationRequested)
            {
                sonHata = $"Bildirim servisi {ayarlar.ZamanAsimiSaniye} saniyede yanıt vermedi (zaman aşımı).";
            }
            catch (HttpRequestException h)
            {
                sonHata = "Bildirim servisine bağlanılamadı.";
                gunluk.LogDebug(h, "ntfy bağlantı hatası");
            }

            gunluk.LogWarning("Bildirim {Id} gönderilemedi (deneme {Deneme}/{Azami}): {Hata}", is_.BildirimId, deneme, ayarlar.AzamiDeneme, sonHata);
            if (deneme < ayarlar.AzamiDeneme)
                await Task.Delay(bekle ?? TimeSpan.FromMilliseconds(ayarlar.BeklemeMilisaniye * deneme), iptal);
        }

        throw new BildirimGonderimHatasi(sonHata, kalici: false);
    }

    // Basarili yanit da ayristirilir: ntfy yayinlanan mesajin kimligini ve "message" olayini dondurur.
    private async Task YanitiDenetle(HttpResponseMessage yanit, HariciBildirimIsi is_, CancellationToken iptal)
    {
        NtfyYaniti? govde;
        try
        {
            govde = await yanit.Content.ReadFromJsonAsync<NtfyYaniti>(iptal);
        }
        catch (System.Text.Json.JsonException)
        {
            govde = null;
        }
        if (govde is not { Event: "message" } || string.IsNullOrEmpty(govde.Id))
            throw new BildirimGonderimHatasi("Bildirim servisinin yanıtı beklenen biçimde değil.", kalici: true);
        gunluk.LogInformation("Bildirim {Id} telefona gönderildi (ntfy kimliği {NtfyId}).", is_.BildirimId, govde.Id);
    }

    private sealed record NtfyIstegi(
        [property: JsonPropertyName("topic")] string Topic,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("message")] string Message,
        [property: JsonPropertyName("tags")] string[] Tags,
        [property: JsonPropertyName("priority")] int Priority);

    private sealed record NtfyYaniti(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("event")] string? Event);
}
