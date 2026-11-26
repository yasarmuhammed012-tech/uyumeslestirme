using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using UyumEslestirme.Domain;

namespace UyumEslestirme.Business;

public sealed record DogrulanmisOneri(string Gerekce, List<SoruKategorisi> GucluYonler, SoruKategorisi? DikkatKonusu, string? DikkatNotu);

public sealed record DogrulamaSonucu(Dictionary<string, DogrulanmisOneri> Oneriler, List<string> Hatalar);

// Yapay zeka ciktisi korlemesine kullanilmaz. Bu sinif ciktiyi veritabanindan hesaplanan gerceklerle
// karsilastirir; tek bir kural bile bozulursa butun cikti reddedilir ve kural tabanli gerekce gosterilir.
public static partial class OneriDogrulayici
{
    public const double GucluSiniri = 60;
    public const double DikkatSiniri = 75;
    public const int AsgariGerekce = 20;
    public const int AzamiGerekce = 400;
    public const int AzamiDikkatNotu = 250;

    [GeneratedRegex(@"%\s?(\d{1,3})|(\d{1,3})\s?%")]
    private static partial Regex YuzdeKalibi();

    // E-posta, web adresi, telefon numarasi ve aday kodu metinde olmamali.
    [GeneratedRegex(@"@|https?://|www\.|\d[\d\s-]{6,}\d|\bA\d\b")]
    private static partial Regex YasakKalip();

    public static DogrulamaSonucu Dogrula(string? json, IReadOnlyList<AdayGercekleri> gercekler)
    {
        var hatalar = new List<string>();
        var sonuc = new Dictionary<string, DogrulanmisOneri>();

        if (string.IsNullOrWhiteSpace(json))
            return new(sonuc, ["yanıt boş"]);

        JsonDocument belge;
        try
        {
            belge = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            return new(sonuc, ["yanıt geçerli JSON değil"]);
        }

        using (belge)
        {
            if (belge.RootElement.ValueKind != JsonValueKind.Object
                || !belge.RootElement.TryGetProperty("oneriler", out var dizi)
                || dizi.ValueKind != JsonValueKind.Array)
            {
                return new(sonuc, ["\"oneriler\" dizisi yok"]);
            }

            var beklenen = gercekler.ToDictionary(g => g.Kod);
            foreach (var oge in dizi.EnumerateArray())
            {
                var kod = Metin(oge, "kod");
                if (kod is null || !beklenen.TryGetValue(kod, out var g))
                {
                    hatalar.Add($"bilinmeyen aday kodu: {kod ?? "(yok)"}");
                    continue;
                }
                if (sonuc.ContainsKey(kod))
                {
                    hatalar.Add($"{kod} iki kez önerilmiş");
                    continue;
                }
                if (Oneri(oge, g, hatalar) is { } oneri)
                    sonuc[kod] = oneri;
            }

            foreach (var kod in beklenen.Keys.Where(k => !sonuc.ContainsKey(k) && !hatalar.Any(h => h.StartsWith(k, StringComparison.Ordinal))))
                hatalar.Add($"{kod} için öneri yok");
        }

        return new(sonuc, hatalar);
    }

    private static DogrulanmisOneri? Oneri(JsonElement oge, AdayGercekleri g, List<string> hatalar)
    {
        var onceki = hatalar.Count;
        var gerekce = Metin(oge, "gerekce")?.Trim() ?? "";
        var not = Metin(oge, "dikkat_notu")?.Trim() ?? "";

        if (gerekce.Length is < AsgariGerekce or > AzamiGerekce)
            hatalar.Add($"{g.Kod} gerekçesi {AsgariGerekce}-{AzamiGerekce} karakter değil");
        if (not.Length > AzamiDikkatNotu)
            hatalar.Add($"{g.Kod} dikkat notu çok uzun");
        if (YasakKalip().IsMatch(gerekce) || YasakKalip().IsMatch(not))
            hatalar.Add($"{g.Kod} metninde iletişim bilgisi veya aday kodu var");

        // Metindeki her yuzde gercek bir degere (genel uyum veya bir kategori uyumu) karsilik gelmeli.
        var gecerliYuzdeler = g.Kategoriler.Select(k => k.Uyum).Append(g.Aday.UyumYuzdesi).Select(y => Math.Round(y)).ToList();
        foreach (Match m in YuzdeKalibi().Matches($"{gerekce} {not}"))
        {
            var sayi = double.Parse(m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value, CultureInfo.InvariantCulture);
            if (!gecerliYuzdeler.Any(y => Math.Abs(y - sayi) <= 1))
                hatalar.Add($"{g.Kod} metnindeki %{sayi} verideki hiçbir değerle uyuşmuyor");
        }

        var guclu = new List<SoruKategorisi>();
        if (oge.TryGetProperty("guclu_yonler", out var gucluDizi) && gucluDizi.ValueKind == JsonValueKind.Array)
        {
            foreach (var k in gucluDizi.EnumerateArray())
            {
                if (k.ValueKind != JsonValueKind.String || !Enum.TryParse<SoruKategorisi>(k.GetString(), out var kategori)
                    || !Enum.IsDefined(kategori))
                {
                    hatalar.Add($"{g.Kod} geçersiz kategori: {k}");
                }
                else if (g.Kategori(kategori).Uyum < GucluSiniri)
                {
                    hatalar.Add($"{g.Kod} için {kategori} güçlü yön değil (uyum %{OneriServisi.Yuzde(g.Kategori(kategori).Uyum)})");
                }
                else if (!guclu.Contains(kategori))
                {
                    guclu.Add(kategori);
                }
            }
        }
        if (guclu.Count is 0 or > 3)
            hatalar.Add($"{g.Kod} için 1-3 güçlü yön olmalı");

        SoruKategorisi? dikkat = null;
        var enDusukIki = g.Kategoriler.OrderBy(k => k.Uyum).Take(2).ToList();
        var dikkatMetni = Metin(oge, "dikkat_konusu");
        if (dikkatMetni == OneriIstemi.Yok)
        {
            if (enDusukIki[0].Uyum < DikkatSiniri)
                hatalar.Add($"{g.Kod} için düşük uyumlu konu (%{OneriServisi.Yuzde(enDusukIki[0].Uyum)}) atlanmış");
        }
        else if (Enum.TryParse<SoruKategorisi>(dikkatMetni, out var k2) && Enum.IsDefined(k2))
        {
            if (!enDusukIki.Any(k => k.Kategori == k2))
                hatalar.Add($"{g.Kod} dikkat konusu en düşük uyumlu kategorilerden değil");
            else if (not.Length == 0)
                hatalar.Add($"{g.Kod} dikkat notu boş");
            dikkat = k2;
        }
        else
        {
            hatalar.Add($"{g.Kod} geçersiz dikkat konusu");
        }

        return hatalar.Count == onceki ? new DogrulanmisOneri(gerekce, guclu, dikkat, dikkat is null ? null : not) : null;
    }

    private static string? Metin(JsonElement oge, string ad) =>
        oge.TryGetProperty(ad, out var d) && d.ValueKind == JsonValueKind.String ? d.GetString() : null;
}
