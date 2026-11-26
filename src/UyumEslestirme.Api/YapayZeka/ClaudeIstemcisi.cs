using System.Text.Json;
using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Beta;
using Anthropic.Models.Beta.Messages;
using UyumEslestirme.Business;

namespace UyumEslestirme.Api.YapayZeka;

public sealed class YapayZekaAyarlari
{
    public const string Bolum = "YapayZeka";

    // Koda ve depoya yazilmaz: ANTHROPIC_API_KEY ortam degiskeni (veya YapayZeka__Anahtar).
    public string Anahtar { get; set; } = "";
    public string Model { get; set; } = "claude-opus-5-5";
}

// Anthropic resmi C# SDK'si ile Claude'a tek istek atar. Cikti, OneriIstemi.Sema ile sinirlanan JSON'dur
// (structured outputs). Model reddederse sunucu tarafi yedek model ("fallbacks") devreye girer.
public sealed class ClaudeIstemcisi(AnthropicClient istemci, YapayZekaAyarlari ayarlar, ILogger<ClaudeIstemcisi> gunluk)
    : IYapayZekaIstemcisi
{
    public async Task<string> JsonUret(string sistem, string istem, string jsonSemasi, CancellationToken iptal)
    {
        var sema = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonSemasi)!;
        BetaMessage yanit;
        try
        {
            yanit = await istemci.Beta.Messages.Create(new MessageCreateParams
            {
                Model = ayarlar.Model,
                MaxTokens = 4000,
                Betas = [AnthropicBeta.ServerSideFallback2026_07_01],
                Fallbacks = new Default(),
                System = sistem,
                OutputConfig = new BetaOutputConfig
                {
                    // Kisa ve tanimli bir is: dusuk efor yeterli, maliyet ve bekleme suresi azalir.
                    Effort = Effort.Low,
                    Format = new BetaJsonOutputFormat { Schema = sema },
                },
                Messages = [new BetaUserMessageParam { Content = istem }],
            }, iptal);
        }
        catch (AnthropicRateLimitException h)
        {
            gunluk.LogWarning(h, "Claude hız sınırı");
            throw new YapayZekaHatasi("hız sınırı aşıldı", h);
        }
        catch (AnthropicApiException h)
        {
            gunluk.LogWarning(h, "Claude API hatası");
            throw new YapayZekaHatasi("servis hata döndürdü", h);
        }
        catch (HttpRequestException h)
        {
            gunluk.LogWarning(h, "Claude bağlantı hatası");
            throw new YapayZekaHatasi("bağlantı kurulamadı", h);
        }

        // Ret veya kesilmis yanit kullanilmaz.
        if (yanit.StopReason != BetaStopReason.EndTurn)
            throw new YapayZekaHatasi($"yanıt tamamlanmadı ({yanit.StopReason})");

        var metin = string.Concat(yanit.Content.Select(b => b.Value).OfType<BetaTextBlock>().Select(t => t.Text));
        gunluk.LogInformation("Claude önerisi alındı: {Giris} giriş, {Cikis} çıkış token", yanit.Usage.InputTokens, yanit.Usage.OutputTokens);
        return metin;
    }
}
