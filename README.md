# Ev/Yurt Arkadaşı Uyum Eşleştirme (P19)

Öğrencilerin yaşam tarzı tercihlerine göre uyumlu ev/yurt arkadaşı bulmasını sağlayan masaüstü uygulaması.

**Ders:** Bilgisayar Uygulamaları I — Bingöl Üniversitesi, Bilgisayar Programcılığı

## Teknoloji

| Alan | Seçim |
| --- | --- |
| Dil / platform | C#, .NET 10 |
| Arayüz | Windows Forms |
| Veritabanı | SQLite (tek dosya, kurulum gerektirmez) |
| Veri erişimi | Entity Framework Core |
| Test | xUnit |

## Çalıştırma

Gereksinim: Windows ve .NET 10 SDK.

```powershell
dotnet run --project src/UyumEslestirme.Desktop
```

İlk açılışta veritabanı otomatik oluşur. Debug derlemesinde sentetik demo hesaplar da eklenir, parolaları `Demo1234`:

- Yönetici: `yonetici@ornek.test`
- Öğrenci: `ogrenci01@ornek.test` ... `ogrenci20@ornek.test`

Testler:

```powershell
dotnet test
```

## Proje yapısı

```text
src/
  UyumEslestirme.Domain/    Varlıklar ve enum'lar (başka katmana bağımlı değil)
  UyumEslestirme.Data/      Veritabanı ve veri erişimi
  UyumEslestirme.Business/  İş kuralları ve doğrulama
  UyumEslestirme.Desktop/   Windows Forms arayüzü
tests/
  UyumEslestirme.Tests/     Birim testleri
docs/                       ER diyagramı, mimari ve haftalık notlar
```

## Belgeler

- [ER diyagramı](docs/er-diyagrami.md)
- [Mimari](docs/mimari.md)
- [Hafta 4: Veritabanı](docs/hafta-4-veritabani.md)
- [Hafta 5: Güvenlik notu](docs/hafta-5-guvenlik-notu.md)

## Haftalık ilerleme

- [x] Hafta 3: Kurulum, mimari ve veri modeli
- [x] Hafta 4: Veritabanı ve veri erişim katmanı
- [x] Hafta 5: Kullanıcı yönetimi ve kimlik doğrulama
