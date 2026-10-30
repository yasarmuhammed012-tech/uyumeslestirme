using UyumEslestirme.Domain;

namespace UyumEslestirme.Data;

// Yalnizca sentetik veri uretir. Adlar ve e-postalar gercek kisilere ait degildir (KVKK).
public static class VeriTohumlayici
{
    public const string GirisKapaliHash = "!giris-kapali";

    private static readonly (SoruKategorisi Kategori, string Metin)[] Sorular =
    [
        (SoruKategorisi.UykuDuzeni, "Geç saatlere kadar uyanık kalırım."),
        (SoruKategorisi.UykuDuzeni, "Sabahları erken kalkarım."),
        (SoruKategorisi.Temizlik, "Ortak alanların her zaman düzenli olmasını isterim."),
        (SoruKategorisi.Temizlik, "Temizlik işlerini düzenli olarak paylaşmak isterim."),
        (SoruKategorisi.Sosyallik, "Eve sık sık misafir gelmesinden rahatsız olmam."),
        (SoruKategorisi.Sosyallik, "Ev arkadaşımla birlikte vakit geçirmeyi severim."),
        (SoruKategorisi.Gurultu, "Çalışırken veya dinlenirken sessizlik isterim."),
        (SoruKategorisi.Gurultu, "Evde yüksek sesle müzik dinlemekten hoşlanırım."),
        (SoruKategorisi.Ders, "Ders çalışmak için evi sık kullanırım."),
        (SoruKategorisi.Ders, "Sınav dönemlerinde evde yoğun çalışırım."),
    ];

    private static readonly string[] Bolumler =
        ["Bilgisayar Programcılığı", "Muhasebe", "Hemşirelik", "Elektrik", "İşletme"];

    private static readonly string[] Sehirler = ["Bingöl", "Elazığ", "Diyarbakır"];

    // Anket sorulari uygulamanin kendi verisidir; demo hesaplardan bagimsiz olarak her kurulumda gerekir.
    public static List<AnketSorusu> SorulariTohumla(UygulamaDbContext db)
    {
        if (db.AnketSorulari.Any())
            return db.AnketSorulari.OrderBy(s => s.Sira).ToList();

        var sorular = Sorular
            .Select((s, i) => new AnketSorusu { Kategori = s.Kategori, Metin = s.Metin, Sira = i + 1 })
            .ToList();
        db.AnketSorulari.AddRange(sorular);
        db.SaveChanges();
        return sorular;
    }

    public static void Tohumla(UygulamaDbContext db, string ogrenciHash, string yoneticiHash, DateTime simdi)
    {
        if (db.Kullanicilar.Any())
            return;

        // Rastgelelik sabit tohumla uretilir; her kurulumda ayni ornek veri olusur.
        var rastgele = new Random(19);

        var sorular = SorulariTohumla(db);

        db.Kullanicilar.Add(new Kullanici
        {
            Eposta = "yonetici@ornek.test",
            AdSoyad = "Örnek Yönetici",
            ParolaHash = yoneticiHash,
            Rol = Rol.Yonetici,
            OlusturmaTarihi = simdi,
        });

        var ogrenciler = new List<Kullanici>();
        for (var i = 1; i <= 20; i++)
        {
            var ogrenci = new Kullanici
            {
                Eposta = $"ogrenci{i:00}@ornek.test",
                AdSoyad = $"Örnek Öğrenci {i:00}",
                ParolaHash = ogrenciHash,
                OlusturmaTarihi = simdi,
                Profil = new Profil
                {
                    Bolum = Bolumler[rastgele.Next(Bolumler.Length)],
                    Sehir = Sehirler[rastgele.Next(Sehirler.Length)],
                    DogumYili = rastgele.Next(2000, 2008),
                },
            };
            foreach (var soru in sorular)
            {
                ogrenci.Cevaplar.Add(new AnketCevabi
                {
                    Soru = soru,
                    Deger = rastgele.Next(1, 6),
                    Agirlik = rastgele.Next(1, 6),
                });
            }
            ogrenciler.Add(ogrenci);
        }
        db.Kullanicilar.AddRange(ogrenciler);

        for (var i = 0; i < 5; i++)
        {
            db.Ilanlar.Add(new Ilan
            {
                Sahip = ogrenciler[i],
                Baslik = $"Örnek ilan {i + 1}: Eve arkadaş aranıyor",
                Aciklama = "Sentetik örnek ilan metni.",
                Sehir = ogrenciler[i].Profil!.Sehir,
                AylikKira = 2000 + rastgele.Next(0, 11) * 250,
                OlusturmaTarihi = simdi,
            });
        }

        db.SaveChanges();
    }
}
