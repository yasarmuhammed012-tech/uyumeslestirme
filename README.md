# Ev/Yurt Arkadaşı Uyum Eşleştirme (P19)

Öğrencilerin yaşam tarzı tercihlerine göre uyumlu ev/yurt arkadaşı bulmasını sağlayan masaüstü uygulaması.

**Ders:** Bilgisayar Uygulamaları I — Bingöl Üniversitesi, Bilgisayar Programcılığı

**Tarayıcıda dene:** https://yasarmuhammed012-tech.github.io/uyumeslestirme/

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

### Web sürümü

Aynı iş ve veri katmanları Blazor WebAssembly arayüzüyle tarayıcıda da çalışır. `main` dalına her gönderimde GitHub Actions testleri çalıştırır ve siteyi GitHub Pages'te yayınlar. Yerelde çalıştırmak için:

```powershell
dotnet run --project src/UyumEslestirme.Web
```

Web sürümü bir demodur, masaüstünden şu açılardan ayrılır:

- **Veri:** SQLite yerine bellek içi veritabanı kullanır. Veriler yalnızca o tarayıcı sekmesinde tutulur, sayfa yenilenince demo verilere döner.
- **Parola özeti:** Tarayıcıda hızlı giriş için PBKDF2 tekrar sayısı düşürülmüştür. Burada gerçek parola kullanılmamalıdır.

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
  UyumEslestirme.Web/       Tarayıcı arayüzü (Blazor WebAssembly)
tests/
  UyumEslestirme.Tests/     Birim testleri
docs/                       ER diyagramı, mimari ve haftalık notlar
```

## Belgeler

- [ER diyagramı](docs/er-diyagrami.md)
- [Mimari](docs/mimari.md)
- [Hafta 4: Veritabanı](docs/hafta-4-veritabani.md)
- [Hafta 5: Güvenlik notu](docs/hafta-5-guvenlik-notu.md)
- [Hafta 6: Profil ve tercih anketi](docs/hafta-6-profil-anket.md)
- [Hafta 7: Uyum skorlama ve eşleştirme](docs/hafta-7-uyum-eslestirme.md)
- [Uyum skoru hesabı](docs/uyum-skoru.md)

## Haftalık ilerleme

- [x] Hafta 3: Kurulum, mimari ve veri modeli
- [x] Hafta 4: Veritabanı ve veri erişim katmanı
- [x] Hafta 5: Kullanıcı yönetimi ve kimlik doğrulama
- [x] Hafta 6: Profil ve tercih anketi
- [x] Hafta 7: Uyum skorlama ve eşleştirme
