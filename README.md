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

## Haftalık ilerleme

- [x] Hafta 3: Kurulum, mimari ve veri modeli
