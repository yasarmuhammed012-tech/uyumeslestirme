using System.Globalization;
using System.Text.Json;
using UyumEslestirme.Domain;

namespace UyumEslestirme.Business;

// Istem (prompt) tasarimi. Ilkeler:
// - Model kimin onerilecegine karar vermez; adaylar skorla secilmis ve siralanmis gelir.
// - Modele yalnizca hesaplanmis sayilar gider: ad, e-posta, bolum, sehir, "hakkinda" metni gitmez.
//   Boylece kisisel veri paylasilmaz ve serbest metinle istem enjeksiyonu yapilamaz.
// - Cikti JSON semasiyla sinirlanir, ardindan OneriDogrulayici gerceklerle karsilastirir.
public static class OneriIstemi
{
    public const string Sistem = """
        Sen bir yurt/ev arkadaşı eşleştirme uygulamasında öneri gerekçesi yazan yardımcısın.
        Kullanıcıya en uyumlu adaylar, uyum skoru formülüyle zaten seçildi ve sıralandı. Senin görevin
        her adayın neden uygun olduğunu kısa ve dürüst biçimde açıklamak. Sıralamayı değiştirme, yeni aday ekleme.

        Kurallar:
        - Yalnızca verilen sayılara dayan. Veride olmayan bir alışkanlık, kişilik özelliği veya bilgi uydurma.
        - Yüzde yazarsan yalnızca verideki değerleri yaz (genel uyum veya kategori uyumu), yuvarlamadan.
        - Aday kodlarını (A1, A2...) gerekçe metninde kullanma; kişiye "bu kişi" ya da "aday" de.
        - "guclu_yonler": uyumu en yüksek 1-3 kategori. Uyumu 60'ın altındaki bir kategoriyi güçlü yön sayma.
        - "dikkat_konusu": uyumu en düşük iki kategoriden biri; en düşük kategori uyumu 75 veya üstündeyse "Yok".
        - "dikkat_notu": dikkat konusu varsa yargılamadan, konuşulması önerilen tek cümle; yoksa boş metin.
        - "gerekce": Türkçe, 1-2 cümle, en fazla 350 karakter.
        - Cinsiyet, din, köken, sağlık gibi konulara hiç değinme.
        """;

    // Kategori kodlari semada enum olarak sabitlenir; model baska deger uretemez.
    public static readonly string[] KategoriKodlari = Enum.GetNames<SoruKategorisi>();
    public const string Yok = "Yok";

    public static string Kullanici(IEnumerable<AdayGercekleri> gercekler)
    {
        var veri = gercekler.Select(g => new
        {
            kod = g.Kod,
            genel_uyum = OneriServisi.Yuzde(g.Aday.UyumYuzdesi),
            kategoriler = g.Kategoriler.Select(k => new
            {
                kod = k.Kategori.ToString(),
                ad = KategoriAdlari.Ad(k.Kategori),
                uyum = OneriServisi.Yuzde(k.Uyum),
                kullanicinin_ortalama_cevabi = k.BenimOrtalama.ToString("0.0", CultureInfo.InvariantCulture),
                adayin_ortalama_cevabi = k.OnunOrtalama.ToString("0.0", CultureInfo.InvariantCulture),
                ortak_onem = k.Onem.ToString("0.0", CultureInfo.InvariantCulture),
            }),
        });

        return $"""
            Cevaplar 1 (hiç katılmıyorum) ile 5 (tamamen katılıyorum) arasındadır; önem 1-5 arasıdır.
            Kategori soruları: Gece düzeni = geç yatma/geç kalkma, Düzen ve temizlik = ortak alan düzeni ve iş paylaşımı,
            Sosyallik = misafir ve birlikte vakit, Sessizlik isteği = sessiz ortam isteği, Evde ders çalışma = evde ders yoğunluğu.

            Adaylar (uyum skoruna göre sıralı):
            {JsonSerializer.Serialize(veri, new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping })}

            Her aday için bir öneri yaz.
            """;
    }

    public static readonly string Sema = JsonSerializer.Serialize(new Dictionary<string, object>
    {
        ["type"] = "object",
        ["properties"] = new Dictionary<string, object>
        {
            ["oneriler"] = new Dictionary<string, object>
            {
                ["type"] = "array",
                ["items"] = new Dictionary<string, object>
                {
                    ["type"] = "object",
                    ["properties"] = new Dictionary<string, object>
                    {
                        ["kod"] = new { type = "string" },
                        ["gerekce"] = new { type = "string" },
                        ["guclu_yonler"] = new { type = "array", items = new { type = "string", @enum = KategoriKodlari } },
                        ["dikkat_konusu"] = new { type = "string", @enum = KategoriKodlari.Append(Yok).ToArray() },
                        ["dikkat_notu"] = new { type = "string" },
                    },
                    ["required"] = new[] { "kod", "gerekce", "guclu_yonler", "dikkat_konusu", "dikkat_notu" },
                    ["additionalProperties"] = false,
                },
            },
        },
        ["required"] = new[] { "oneriler" },
        ["additionalProperties"] = false,
    });
}
